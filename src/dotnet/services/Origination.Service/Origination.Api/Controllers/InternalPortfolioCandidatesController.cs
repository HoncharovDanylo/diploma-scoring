using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Origination.Domain;
using Origination.Infrastructure.Persistence;

namespace Origination.Api.Controllers;

[ApiController]
[Route("internal/v1/portfolio")]
public sealed class InternalPortfolioCandidatesController : ControllerBase
{
    private readonly OriginationDbContext _db;

    public InternalPortfolioCandidatesController(OriginationDbContext db) => _db = db;

    [HttpGet("candidates")]
    public async Task<ActionResult> GetCandidates([FromQuery] DateOnly businessDate, CancellationToken cancellationToken)
    {
        _ = businessDate;

        var apps = await _db.LoanApplications
            .AsNoTracking()
            .Include(x => x.ScoringAttempts)
            .ThenInclude(x => x.Result)
            .Where(x => x.Status == ApplicationStatuses.ManualVerificationPending)
            .ToListAsync(cancellationToken);

        var rows = apps.Select(x =>
        {
            var lastPd = x.ScoringAttempts
                .Where(a => a.Status == "Succeeded" && a.Result != null)
                .OrderByDescending(a => a.CompletedAtUtc)
                .Select(a => a.Result!.ProbabilityOfDefault)
                .FirstOrDefault();
            var expectedProfit = x.CalculatedRepaymentAmount > 0m
                ? decimal.Round(x.CalculatedRepaymentAmount - x.RequestedPrincipal, 2, MidpointRounding.AwayFromZero)
                : decimal.Round(
                    x.RequestedPrincipal * x.AppliedInterestRatePerDay * x.RequestedTermDays,
                    2,
                    MidpointRounding.AwayFromZero);

            var maxPrincipal = x.RequestedPrincipal;
            var minPrincipal = x.ProductMinPrincipal;
            return new
            {
                applicationId = x.ApplicationId,
                requestedPrincipal = x.RequestedPrincipal.ToString("F2", System.Globalization.CultureInfo.InvariantCulture),
                minPrincipal = minPrincipal.ToString("F2", System.Globalization.CultureInfo.InvariantCulture),
                maxPrincipal = maxPrincipal.ToString("F2", System.Globalization.CultureInfo.InvariantCulture),
                probabilityOfDefault = lastPd.ToString("F6", System.Globalization.CultureInfo.InvariantCulture),
                expectedProfit = expectedProfit.ToString("F2", System.Globalization.CultureInfo.InvariantCulture)
            };
        }).ToList();

        return Ok(new { businessDate, candidates = rows });
    }

    [HttpGet("issued-summary")]
    public async Task<ActionResult> GetIssuedSummary([FromQuery] DateOnly businessDate, CancellationToken cancellationToken)
    {
        var issuedPrincipal = await _db.LoanApplications
            .AsNoTracking()
            .Where(x =>
                x.Status == ApplicationStatuses.ManualApproved &&
                DateOnly.FromDateTime(x.UpdatedAtUtc) == businessDate)
            .SumAsync(
                x => (decimal?)(x.ApprovedPrincipal ?? x.RequestedPrincipal),
                cancellationToken) ?? 0m;

        return Ok(new
        {
            businessDate,
            issuedPrincipal
        });
    }
}
