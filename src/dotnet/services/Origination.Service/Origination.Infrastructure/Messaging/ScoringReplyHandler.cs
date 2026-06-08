using System.Text.Json;
using Lending.Contracts.Events;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Origination.Domain;
using Origination.Domain.Entities;
using Origination.Infrastructure.Persistence;

namespace Origination.Infrastructure.Messaging;

public sealed class ScoringReplyHandler
{
    private readonly OriginationDbContext _db;
    private readonly ILogger<ScoringReplyHandler> _logger;

    public ScoringReplyHandler(OriginationDbContext db, ILogger<ScoringReplyHandler> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task HandleAsync(string envelopeJson, CancellationToken cancellationToken)
    {
        var envelope = IntegrationEnvelope.Deserialize(envelopeJson);
        if (envelope is null)
        {
            _logger.LogWarning("Invalid integration envelope JSON");
            return;
        }

        var opts = IntegrationEnvelope.GetSerializerOptions();

        switch (envelope.EventType)
        {
            case EventTypes.ScoringCompleted:
                var ok = JsonSerializer.Deserialize<ScoringCompletedPayload>(envelope.Payload.GetRawText(), opts);
                if (ok is not null)
                    await ApplyCompletedAsync(ok, cancellationToken);
                break;
            case EventTypes.ScoringFailed:
                var fail = JsonSerializer.Deserialize<ScoringFailedPayload>(envelope.Payload.GetRawText(), opts);
                if (fail is not null)
                    await ApplyFailedAsync(fail, cancellationToken);
                break;
            default:
                _logger.LogDebug("Ignoring event type {Type}", envelope.EventType);
                break;
        }
    }

    private async Task ApplyCompletedAsync(ScoringCompletedPayload p, CancellationToken ct)
    {
        var attempt = await _db.ScoringAttempts
            .Include(x => x.LoanApplication)
            .FirstOrDefaultAsync(x => x.ScoringAttemptId == p.ScoringAttemptId, ct);

        if (attempt is null)
        {
            _logger.LogWarning("ScoringAttempt {Id} not found", p.ScoringAttemptId);
            return;
        }

        if (attempt.Status == "Succeeded")
            return;

        if (!decimal.TryParse(p.ProbabilityOfDefault, System.Globalization.NumberStyles.Any,
                System.Globalization.CultureInfo.InvariantCulture, out var pd))
            pd = 0m;

        var newStatus = MapFinalDecisionToStatus(p.FinalDecision);
        var now = DateTime.UtcNow;
        var app = attempt.LoanApplication;

        attempt.Status = "Succeeded";
        attempt.CompletedAtUtc = now;

        var explanation = JsonSerializer.Serialize(p.TopFactors, IntegrationEnvelope.GetSerializerOptions());

        _db.ScoringResults.Add(new ScoringResult
        {
            ScoringResultId = Guid.NewGuid(),
            ScoringAttemptId = attempt.ScoringAttemptId,
            ProbabilityOfDefault = pd,
            FinalDecision = p.FinalDecision,
            ModelId = p.ModelId,
            ModelVersion = p.ModelVersion,
            ExplanationJson = explanation
        });

        _db.ApplicationStatusHistories.Add(new ApplicationStatusHistory
        {
            HistoryId = Guid.NewGuid(),
            ApplicationId = app.ApplicationId,
            FromStatus = app.Status,
            ToStatus = newStatus,
            ChangedAtUtc = now,
            ChangedByUserId = null,
            ReasonCode = "ScoringCompleted"
        });

        app.Status = newStatus;
        app.UpdatedAtUtc = now;

        await _db.SaveChangesAsync(ct);
    }

    private async Task ApplyFailedAsync(ScoringFailedPayload p, CancellationToken ct)
    {
        var attempt = await _db.ScoringAttempts
            .Include(x => x.LoanApplication)
            .FirstOrDefaultAsync(x => x.ScoringAttemptId == p.ScoringAttemptId, ct);

        if (attempt is null)
            return;

        if (attempt.Status == "Succeeded")
            return;

        var now = DateTime.UtcNow;
        var app = attempt.LoanApplication;
        var newStatus = ApplicationStatuses.ScoringFailed;

        attempt.Status = "Failed";
        attempt.CompletedAtUtc = now;

        _db.ApplicationStatusHistories.Add(new ApplicationStatusHistory
        {
            HistoryId = Guid.NewGuid(),
            ApplicationId = app.ApplicationId,
            FromStatus = app.Status,
            ToStatus = newStatus,
            ChangedAtUtc = now,
            ReasonCode = p.ErrorCode
        });

        app.Status = newStatus;
        app.UpdatedAtUtc = now;

        await _db.SaveChangesAsync(ct);
    }

    private static string MapFinalDecisionToStatus(string finalDecision) => finalDecision switch
    {
        "Approve" => ApplicationStatuses.ManualVerificationPending,
        "Reject" => ApplicationStatuses.ScoringRejectedFinal,
        "ManualReview" => ApplicationStatuses.ManualVerificationPending,
        _ => ApplicationStatuses.ManualVerificationPending
    };
}
