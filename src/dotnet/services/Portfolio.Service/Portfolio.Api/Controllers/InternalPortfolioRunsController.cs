using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Portfolio.Infrastructure;

namespace Portfolio.Api.Controllers;

[ApiController]
[Route("internal/v1/portfolio-runs")]
public sealed class InternalPortfolioRunsController : ControllerBase
{
    private readonly PortfolioDbContext _db;
    private readonly OriginationReadClient _origination;

    public InternalPortfolioRunsController(PortfolioDbContext db, OriginationReadClient origination)
    {
        _db = db;
        _origination = origination;
    }

    [HttpGet("{portfolioRunId:guid}/optimization-input")]
    public async Task<ActionResult> GetOptimizationInput(Guid portfolioRunId, CancellationToken cancellationToken)
    {
        var run = await _db.PortfolioOptimizationRuns.AsNoTracking()
            .FirstOrDefaultAsync(x => x.PortfolioRunId == portfolioRunId, cancellationToken);
        if (run is null)
            return NotFound();

        var candidatesDoc = await _origination.GetPortfolioCandidatesAsync(run.BusinessDate, cancellationToken);
        var budget = run.BudgetCapSnapshot.ToString("F2", System.Globalization.CultureInfo.InvariantCulture);

        object? candidates = null;
        if (candidatesDoc.RootElement.TryGetProperty("candidates", out var arr))
            candidates = JsonSerializer.Deserialize<object>(arr.GetRawText());

        return Ok(new
        {
            portfolioRunId,
            businessDate = run.BusinessDate.ToString("yyyy-MM-dd"),
            budgetCap = budget,
            candidates
        });
    }
}
