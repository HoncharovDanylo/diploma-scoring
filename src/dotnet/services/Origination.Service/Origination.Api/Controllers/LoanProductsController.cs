using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Origination.Domain.Entities;
using Origination.Infrastructure.Persistence;

namespace Origination.Api.Controllers;

[ApiController]
[Route("api/v1/loan-products")]
[Authorize(Roles = "Admin")]
public sealed class LoanProductsController : ControllerBase
{
    private readonly OriginationDbContext _db;

    public LoanProductsController(OriginationDbContext db) => _db = db;

    public sealed class CreateLoanProductRequest
    {
        public string ProductCode { get; set; } = "STD-LOAN";
        public decimal MinPrincipal { get; set; }
        public decimal MaxPrincipal { get; set; }
        public int MinTermDays { get; set; }
        public int MaxTermDays { get; set; }
        public decimal InterestRatePerDay { get; set; }
        public bool Activate { get; set; } = true;
    }

    [HttpGet]
    public async Task<ActionResult> List(CancellationToken cancellationToken)
    {
        var rows = await _db.LoanProducts
            .AsNoTracking()
            .OrderBy(x => x.ProductCode)
            .ThenByDescending(x => x.Version)
            .Select(x => new
            {
                x.LoanProductId,
                x.ProductCode,
                x.Version,
                x.IsActive,
                x.MinPrincipal,
                x.MaxPrincipal,
                x.MinTermDays,
                x.MaxTermDays,
                x.InterestRatePerDay,
                x.UpdatedAtUtc
            })
            .ToListAsync(cancellationToken);

        return Ok(rows);
    }

    [HttpPost]
    public async Task<ActionResult> Create([FromBody] CreateLoanProductRequest request, CancellationToken cancellationToken)
    {
        ValidateRequest(request);

        var code = request.ProductCode.Trim().ToUpperInvariant();
        var nextVersion = (await _db.LoanProducts
            .Where(x => x.ProductCode == code)
            .MaxAsync(x => (int?)x.Version, cancellationToken) ?? 0) + 1;

        var now = DateTime.UtcNow;
        var row = new LoanProduct
        {
            LoanProductId = Guid.NewGuid(),
            ProductCode = code,
            Version = nextVersion,
            IsActive = request.Activate,
            MinPrincipal = request.MinPrincipal,
            MaxPrincipal = request.MaxPrincipal,
            MinTermDays = request.MinTermDays,
            MaxTermDays = request.MaxTermDays,
            InterestRatePerDay = request.InterestRatePerDay,
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        };

        if (request.Activate)
        {
            var activeRows = await _db.LoanProducts
                .Where(x => x.ProductCode == code && x.IsActive)
                .ToListAsync(cancellationToken);
            foreach (var active in activeRows)
            {
                active.IsActive = false;
                active.UpdatedAtUtc = now;
            }
        }

        _db.LoanProducts.Add(row);
        await _db.SaveChangesAsync(cancellationToken);
        return CreatedAtAction(nameof(List), new { id = row.LoanProductId }, new { row.LoanProductId, row.ProductCode, row.Version });
    }

    [HttpPut("{loanProductId:guid}/activate")]
    public async Task<ActionResult> Activate(Guid loanProductId, CancellationToken cancellationToken)
    {
        var row = await _db.LoanProducts.FirstOrDefaultAsync(x => x.LoanProductId == loanProductId, cancellationToken);
        if (row is null)
            return NotFound();

        var now = DateTime.UtcNow;
        var siblings = await _db.LoanProducts
            .Where(x => x.ProductCode == row.ProductCode && x.IsActive)
            .ToListAsync(cancellationToken);
        foreach (var sibling in siblings)
        {
            sibling.IsActive = false;
            sibling.UpdatedAtUtc = now;
        }

        row.IsActive = true;
        row.UpdatedAtUtc = now;
        await _db.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    private static void ValidateRequest(CreateLoanProductRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.ProductCode))
            throw new InvalidOperationException("ProductCode is required.");
        if (request.MinPrincipal <= 0m || request.MaxPrincipal <= 0m || request.MinPrincipal > request.MaxPrincipal)
            throw new InvalidOperationException("Principal range is invalid.");
        if (request.MinTermDays <= 0 || request.MaxTermDays <= 0 || request.MinTermDays > request.MaxTermDays)
            throw new InvalidOperationException("Term range is invalid.");
        if (request.InterestRatePerDay <= 0m)
            throw new InvalidOperationException("InterestRatePerDay must be positive.");
    }
}
