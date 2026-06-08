using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Portfolio.Application;
using Portfolio.Infrastructure;

namespace Portfolio.Api.Controllers;

[ApiController]
[Route("api/v1/portfolio")]
[Authorize(Roles = "Admin")]
public sealed class PortfolioRunsController : ControllerBase
{
    private readonly IPortfolioRunsService _runs;
    private readonly PortfolioDbContext _db;
    private readonly OriginationReadClient _origination;

    public PortfolioRunsController(
        IPortfolioRunsService runs,
        PortfolioDbContext db,
        OriginationReadClient origination)
    {
        _runs = runs;
        _db = db;
        _origination = origination;
    }

    public sealed class StartRunRequest
    {
        public DateOnly BusinessDate { get; set; }
    }

    [HttpPost("runs")]
    public async Task<ActionResult> StartRun([FromBody] StartRunRequest dto, CancellationToken cancellationToken)
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var correlationId = HttpContext.Request.Headers.TryGetValue("X-Correlation-Id", out var c)
            ? c.ToString()
            : Guid.NewGuid().ToString("N");
        var runId = await _runs.StartRunAsync(dto.BusinessDate, userId, correlationId, cancellationToken);
        return Accepted(new { portfolioRunId = runId });
    }

    [HttpGet("runs/{portfolioRunId:guid}")]
    public async Task<ActionResult> GetRun(Guid portfolioRunId, CancellationToken cancellationToken)
    {
        var run = await _db.PortfolioOptimizationRuns
            .AsNoTracking()
            .Include(x => x.Selections)
            .FirstOrDefaultAsync(x => x.PortfolioRunId == portfolioRunId, cancellationToken);
        if (run is null) return NotFound();

        IReadOnlyDictionary<Guid, OriginationVerificationRow> orig = new Dictionary<Guid, OriginationVerificationRow>();
        if (run.Selections.Count > 0)
        {
            try
            {
                orig = await _origination.GetVerificationBatchAsync(
                    run.Selections.Select(s => s.ApplicationId).ToList(),
                    cancellationToken);
            }
            catch
            {
            }
        }

        return Ok(new
        {
            run.PortfolioRunId,
            run.BusinessDate,
            run.Status,
            run.BudgetCapSnapshot,
            run.ObjectiveValue,
            run.UsedBudget,
            run.ExpectedPortfolioProfit,
            Selections = run.Selections.Select(s =>
            {
                orig.TryGetValue(s.ApplicationId, out var o);
                return new
                {
                    s.ApplicationId,
                    recommendedPrincipal = s.SelectedPrincipal,
                    s.ExpectedProfitSnapshot,
                    requestedPrincipal = o?.RequestedPrincipal,
                    productMinPrincipal = o?.ProductMinPrincipal,
                    applicationStatus = o?.Status,
                    approvedPrincipal = o?.ApprovedPrincipal
                };
            })
        });
    }
}
