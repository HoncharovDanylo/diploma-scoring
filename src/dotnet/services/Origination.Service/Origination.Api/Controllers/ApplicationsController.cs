using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Origination.Application;
using Origination.Domain;
using Origination.Infrastructure;
using Origination.Infrastructure.Persistence;
using IntegrationEnvelope = Lending.Contracts.Events.IntegrationEnvelope;

namespace Origination.Api.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
[Authorize]
public sealed class ApplicationsController : ControllerBase
{
    private readonly ILoanOriginationService _origination;
    private readonly OriginationDbContext _db;

    public ApplicationsController(
        ILoanOriginationService origination,
        OriginationDbContext db)
    {
        _origination = origination;
        _db = db;
    }

    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult> List(CancellationToken cancellationToken)
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var isAdmin = User.IsInRole("Admin");
        var q = _db.LoanApplications.AsNoTracking().AsQueryable();
        if (!isAdmin)
            q = q.Where(x => x.CreatedByUserId == userId);

        var list = await q
            .OrderByDescending(x => x.CreatedAtUtc)
            .Select(x => new
            {
                x.ApplicationId,
                x.Status,
                x.RequestedPrincipal,
                x.ApprovedPrincipal,
                x.ProductMinPrincipal,
                x.RequestedTermDays,
                x.CalculatedRepaymentAmount,
                x.AppliedInterestRatePerDay,
                x.Purpose,
                x.CreatedAtUtc
            })
            .ToListAsync(cancellationToken);
        return Ok(list);
    }

    [HttpGet("config")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult> GetCreateConfig(CancellationToken cancellationToken)
    {
        var product = await _db.LoanProducts
            .AsNoTracking()
            .Where(x => x.IsActive)
            .OrderByDescending(x => x.UpdatedAtUtc)
            .FirstOrDefaultAsync(cancellationToken);
        if (product is null)
            throw new InvalidOperationException("No active loan product configured.");

        return Ok(new
        {
            productCode = product.ProductCode,
            productVersion = product.Version,
            minPrincipal = product.MinPrincipal,
            maxPrincipal = product.MaxPrincipal,
            minTermDays = product.MinTermDays,
            maxTermDays = product.MaxTermDays,
            interestRatePerDay = product.InterestRatePerDay
        });
    }

    [HttpPost]
    [ProducesResponseType(typeof(CreateApplicationResponseDto), StatusCodes.Status201Created)]
    public async Task<ActionResult<CreateApplicationResponseDto>> Create(
        [FromBody] CreateApplicationRequestDto request,
        CancellationToken cancellationToken)
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        if (User.IsInRole("Admin"))
            return Forbid();

        var correlationId = HttpContext.Request.Headers.TryGetValue("X-Correlation-Id", out var cid)
            ? cid.ToString()
            : Guid.NewGuid().ToString("N");

        var result = await _origination.CreateApplicationAsync(request, userId, correlationId, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { applicationId = result.ApplicationId }, result);
    }

    [HttpGet("{applicationId:guid}")]
    public async Task<ActionResult> GetById(Guid applicationId, CancellationToken cancellationToken)
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var isAdmin = User.IsInRole("Admin");
        var app = await _db.LoanApplications
            .AsNoTracking()
            .Include(x => x.Applicant)
            .Include(x => x.ScoringAttempts)
            .ThenInclude(x => x.Result)
            .FirstOrDefaultAsync(x => x.ApplicationId == applicationId, cancellationToken);

        if (app is null)
            return NotFound();

        if (app.CreatedByUserId != userId && !isAdmin)
            return Forbid();

        var scoring = app.ScoringAttempts
            .OrderByDescending(x => x.StartedAtUtc)
            .Select(a =>
            {
                if (a.Result is null)
                    return new
                    {
                        a.ScoringAttemptId,
                        a.Status,
                        Result = (object?)null
                    };

                var topFactors = string.IsNullOrWhiteSpace(a.Result.ExplanationJson)
                    ? (object?[]?)null
                    : JsonSerializer.Deserialize<object[]>(a.Result.ExplanationJson, IntegrationEnvelope.GetSerializerOptions());

                object result = isAdmin
                    ? new
                    {
                        a.Result.ProbabilityOfDefault,
                        a.Result.FinalDecision,
                        a.Result.ModelId,
                        a.Result.ModelVersion,
                        TopFactors = topFactors
                    }
                    : new
                    {
                        a.Result.ProbabilityOfDefault,
                        a.Result.FinalDecision,
                        Explanation = ToClientExplanation(
                            a.Result.FinalDecision,
                            a.Result.ProbabilityOfDefault,
                            a.Result.ModelId,
                            a.Result.ExplanationJson)
                    };

                return new
                {
                    a.ScoringAttemptId,
                    a.Status,
                    Result = (object?)result
                };
            });

        return Ok(new
        {
            app.ApplicationId,
            app.Status,
            app.RequestedPrincipal,
            app.ApprovedPrincipal,
            app.ProductMinPrincipal,
            app.RequestedTermDays,
            app.CalculatedRepaymentAmount,
            app.AppliedInterestRatePerDay,
            app.ProductCode,
            app.ProductVersion,
            app.Purpose,
            Applicant = new { app.Applicant.FullName },
            Scoring = scoring
        });
    }

    private static string ToClientExplanation(string finalDecision, decimal pd, string modelId, string? explanationJson)
    {
        if (string.Equals(modelId, "rules-engine", StringComparison.OrdinalIgnoreCase))
        {
            var ruleMessage = TryExtractRuleMessage(explanationJson);
            if (!string.IsNullOrWhiteSpace(ruleMessage))
                return ruleMessage;
        }

        return finalDecision switch
        {
            "Approve" => "Заявку попередньо схвалено і передано на ручну верифікацію.",
            "Reject" => pd >= 0.5m
                ? "На жаль, наразі ми не можемо схвалити цю заявку через високий кредитний ризик."
                : "На жаль, наразі ми не можемо схвалити цю заявку. Спробуйте змінити суму або термін.",
            _ => "Заявка обробляється, очікуйте фінального рішення."
        };
    }

    private static string? TryExtractRuleMessage(string? explanationJson)
    {
        if (string.IsNullOrWhiteSpace(explanationJson))
            return null;
        try
        {
            using var doc = JsonDocument.Parse(explanationJson);
            if (doc.RootElement.ValueKind != JsonValueKind.Array)
                return null;
            foreach (var item in doc.RootElement.EnumerateArray())
            {
                if (item.TryGetProperty("message", out var msg) && msg.ValueKind == JsonValueKind.String)
                    return msg.GetString();
            }
        }
        catch
        {
            return null;
        }

        return null;
    }
}
