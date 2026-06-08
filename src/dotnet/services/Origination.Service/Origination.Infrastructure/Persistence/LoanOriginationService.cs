using Lending.Contracts.Events;
using Microsoft.EntityFrameworkCore;
using Origination.Application;
using Origination.Application.Identity;
using Origination.Domain;
using Origination.Domain.Entities;
using System.Text.Json;

namespace Origination.Infrastructure.Persistence;

public sealed class LoanOriginationService : ILoanOriginationService
{
    private readonly OriginationDbContext _db;
    private readonly IIdentityRiskProfileClient _identity;

    public LoanOriginationService(
        OriginationDbContext db,
        IIdentityRiskProfileClient identity)
    {
        _db = db;
        _identity = identity;
    }

    public async Task<CreateApplicationResponseDto> CreateApplicationAsync(
        CreateApplicationRequestDto request,
        Guid userId,
        string correlationId,
        CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        var applicantId = Guid.NewGuid();
        var applicationId = Guid.NewGuid();
        var attemptId = Guid.NewGuid();

        var product = await _db.LoanProducts
            .AsNoTracking()
            .Where(x => x.IsActive)
            .OrderByDescending(x => x.UpdatedAtUtc)
            .FirstOrDefaultAsync(cancellationToken);
        if (product is null)
            throw new InvalidOperationException("No active loan product configured.");

        if (request.RequestedTermDays < product.MinTermDays || request.RequestedTermDays > product.MaxTermDays)
            throw new InvalidOperationException(
                $"RequestedTermDays must be between {product.MinTermDays} and {product.MaxTermDays}.");
        if (request.RequestedPrincipal < product.MinPrincipal || request.RequestedPrincipal > product.MaxPrincipal)
            throw new InvalidOperationException(
                $"RequestedPrincipal must be between {product.MinPrincipal:F2} and {product.MaxPrincipal:F2}.");

        var repaymentAmount = decimal.Round(
            request.RequestedPrincipal * (1m + (product.InterestRatePerDay * request.RequestedTermDays)),
            2,
            MidpointRounding.AwayFromZero);

        var risk = await _identity.GetAsync(userId, cancellationToken);

        var applicant = new Applicant
        {
            ApplicantId = applicantId,
            UserId = userId,
            FullName = string.IsNullOrWhiteSpace(risk?.DisplayName) ? "Customer" : risk!.DisplayName!,
            TaxIdMasked = risk?.TaxIdMasked,
            DateOfBirth = risk?.DateOfBirth,
            MonthlyIncome = risk?.MonthlyIncome,
            EmploymentStatus = risk?.EmploymentStatus,
            CreatedAtUtc = now,
            UpdatedAtUtc = now,
            CreatedByUserId = userId,
            UpdatedByUserId = userId
        };

        var application = new LoanApplication
        {
            ApplicationId = applicationId,
            ApplicantId = applicantId,
            RequestedPrincipal = request.RequestedPrincipal,
            RequestedTermDays = request.RequestedTermDays,
            AppliedInterestRatePerDay = product.InterestRatePerDay,
            CalculatedRepaymentAmount = repaymentAmount,
            ProductCode = product.ProductCode,
            ProductVersion = product.Version,
            ProductMinPrincipal = product.MinPrincipal,
            ProductMaxPrincipal = product.MaxPrincipal,
            ProductMinTermDays = product.MinTermDays,
            ProductMaxTermDays = product.MaxTermDays,
            Purpose = request.Purpose,
            Status = ApplicationStatuses.ScoringPending,
            CreatedAtUtc = now,
            UpdatedAtUtc = now,
            CreatedByUserId = userId,
            UpdatedByUserId = userId
        };

        var preRuleRejectReason = EvaluatePreScoringRejectReason(applicant, request.RequestedPrincipal, now);
        var initialStatus = preRuleRejectReason is null
            ? ApplicationStatuses.ScoringPending
            : ApplicationStatuses.ScoringRejectedFinal;
        application.Status = initialStatus;

        var history = new ApplicationStatusHistory
        {
            HistoryId = Guid.NewGuid(),
            ApplicationId = applicationId,
            FromStatus = null,
            ToStatus = initialStatus,
            ChangedAtUtc = now,
            ChangedByUserId = userId,
            ReasonCode = preRuleRejectReason is null ? "Submitted" : "PolicyAutoReject"
        };

        var attempt = new ScoringAttempt
        {
            ScoringAttemptId = attemptId,
            ApplicationId = applicationId,
            IdempotencyKey = attemptId.ToString("N"),
            Status = preRuleRejectReason is null ? "Pending" : "Completed",
            CorrelationId = correlationId,
            StartedAtUtc = now,
            CompletedAtUtc = preRuleRejectReason is null ? null : now
        };

        IntegrationOutboxMessage? outbox = null;
        ScoringResult? ruleResult = null;

        if (preRuleRejectReason is null)
        {
            var payload = new ScoringRequestedPayload
            {
                ApplicationId = applicationId,
                ScoringAttemptId = attemptId,
                RequestedAtUtc = DateTimeOffset.UtcNow
            };

            var envelope = IntegrationEnvelope.Create(
                EventTypes.ScoringRequested,
                "Origination",
                schemaVersion: 1,
                payload,
                correlationId);

            outbox = new IntegrationOutboxMessage
            {
                EventType = EventTypes.ScoringRequested,
                EnvelopeJson = envelope.SerializeToJson(),
                CreatedAtUtc = now
            };
        }
        else
        {
            var explanation = JsonSerializer.Serialize(new[]
            {
                new
                {
                    featureName = "policy_rule",
                    contribution = "reject",
                    direction = "down",
                    message = preRuleRejectReason
                }
            });
            ruleResult = new ScoringResult
            {
                ScoringResultId = Guid.NewGuid(),
                ScoringAttemptId = attemptId,
                ProbabilityOfDefault = 1m,
                FinalDecision = "Reject",
                ModelId = "rules-engine",
                ModelVersion = "1.0.0",
                ExplanationJson = explanation
            };
        }

        await using var tx = await _db.Database.BeginTransactionAsync(cancellationToken);
        _db.Applicants.Add(applicant);
        _db.LoanApplications.Add(application);
        _db.ApplicationStatusHistories.Add(history);
        _db.ScoringAttempts.Add(attempt);
        if (ruleResult is not null)
            _db.ScoringResults.Add(ruleResult);
        if (outbox is not null)
            _db.IntegrationOutbox.Add(outbox);
        await _db.SaveChangesAsync(cancellationToken);
        await tx.CommitAsync(cancellationToken);

        return new CreateApplicationResponseDto
        {
            ApplicationId = applicationId,
            ScoringAttemptId = attemptId,
            CorrelationId = correlationId
        };
    }

    private static string? EvaluatePreScoringRejectReason(Applicant applicant, decimal requestedPrincipal, DateTime now)
    {
        if (applicant.DateOfBirth is { } dob && GetAgeYears(dob, now) < 18)
            return "Автовідхилення: вік заявника менше 18 років.";

        if (string.Equals(applicant.EmploymentStatus, "Unemployed", StringComparison.OrdinalIgnoreCase)
            && requestedPrincipal > 20000m)
        {
            return "Автовідхилення: для статусу зайнятості Unemployed максимальна сума становить 20 000.";
        }

        if (applicant.MonthlyIncome is { } income && income > 0m)
        {
            var maxAffordablePrincipal = income * 24m;
            if (requestedPrincipal > maxAffordablePrincipal)
            {
                return $"Автовідхилення: сума заявки перевищує ліміт 24x від місячного доходу ({maxAffordablePrincipal:F2}).";
            }
        }

        return null;
    }

    private static int GetAgeYears(DateOnly birthDate, DateTime nowUtc)
    {
        var today = DateOnly.FromDateTime(nowUtc);
        var age = today.Year - birthDate.Year;
        if (today < birthDate.AddYears(age))
            age--;
        return age;
    }
}
