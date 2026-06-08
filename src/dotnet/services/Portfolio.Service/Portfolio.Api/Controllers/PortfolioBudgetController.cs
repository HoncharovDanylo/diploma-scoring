using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Portfolio.Domain;
using Portfolio.Infrastructure;

namespace Portfolio.Api.Controllers;

[ApiController]
[Route("api/v1/portfolio")]
[Authorize(Roles = "Admin")]
public sealed class PortfolioBudgetController : ControllerBase
{
    private readonly PortfolioDbContext _db;
    private readonly OriginationReadClient _origination;

    public PortfolioBudgetController(PortfolioDbContext db, OriginationReadClient origination)
    {
        _db = db;
        _origination = origination;
    }

    public sealed class PutBudgetRequest
    {
        public DateOnly BusinessDate { get; set; }
        public decimal BudgetCap { get; set; }
    }

    [HttpPut("daily-budget")]
    public async Task<ActionResult> PutDailyBudget([FromBody] PutBudgetRequest dto, CancellationToken cancellationToken)
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var now = DateTime.UtcNow;
        var row = await _db.DailyBudgetPolicies.FirstOrDefaultAsync(x => x.BusinessDate == dto.BusinessDate, cancellationToken);
        if (row is null)
        {
            row = new DailyBudgetPolicy { BusinessDate = dto.BusinessDate, CreatedAtUtc = now, UpdatedAtUtc = now };
            _db.DailyBudgetPolicies.Add(row);
        }

        row.BudgetCap = dto.BudgetCap;
        row.UpdatedAtUtc = now;
        row.UpdatedByUserId = userId;
        await _db.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    [HttpGet("summary/{businessDate}")]
    public async Task<ActionResult> GetSummary([FromRoute] DateOnly businessDate, CancellationToken cancellationToken)
    {
        var totalLimit = await _db.DailyBudgetPolicies
            .Where(x => x.BusinessDate == businessDate)
            .Select(x => (decimal?)x.BudgetCap)
            .FirstOrDefaultAsync(cancellationToken) ?? 0m;

        var issuedToday = await _origination.GetIssuedPrincipalAsync(businessDate, cancellationToken);

        var available = Math.Max(0m, totalLimit - issuedToday);
        return Ok(new
        {
            businessDate,
            totalLimit,
            issuedToday,
            availableBudget = available
        });
    }
}
