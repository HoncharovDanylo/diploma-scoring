using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Origination.Domain;
using Origination.Domain.Entities;
using Origination.Infrastructure.Persistence;

namespace Origination.Api.Controllers;

[ApiController]
[Route("api/v1/manual-verification")]
[Authorize(Roles = "Admin")]
public sealed class ManualVerificationController : ControllerBase
{
    private readonly OriginationDbContext _db;

    public ManualVerificationController(OriginationDbContext db) => _db = db;

    public sealed class DecisionRequest
    {
        public string Decision { get; set; } = "";
        public string? Reason { get; set; }
        public decimal? ApprovedPrincipal { get; set; }
    }

    [HttpGet("pending")]
    public async Task<ActionResult> GetPending(CancellationToken cancellationToken)
    {
        var rows = await _db.LoanApplications
            .AsNoTracking()
            .Include(x => x.ScoringAttempts)
            .ThenInclude(x => x.Result)
            .Where(x => x.Status == ApplicationStatuses.ManualVerificationPending)
            .OrderBy(x => x.CreatedAtUtc)
            .Select(x => new
            {
                x.ApplicationId,
                x.RequestedPrincipal,
                x.ProductMinPrincipal,
                x.ProductMaxPrincipal,
                x.RequestedTermDays,
                x.Purpose,
                x.CreatedAtUtc,
                Applicant = new
                {
                    x.Applicant.FullName,
                    x.Applicant.MonthlyIncome,
                    x.Applicant.EmploymentStatus
                },
                Scoring = x.ScoringAttempts
                    .OrderByDescending(a => a.CompletedAtUtc)
                    .Select(a => a.Result == null
                        ? null
                        : new
                        {
                            a.Result.ProbabilityOfDefault,
                            a.Result.FinalDecision,
                            a.Result.ModelId,
                            a.Result.ModelVersion
                        })
                    .FirstOrDefault()
            })
            .ToListAsync(cancellationToken);

        return Ok(rows);
    }

    [HttpPost("{applicationId:guid}/decision")]
    public async Task<ActionResult> ApplyDecision(
        [FromRoute] Guid applicationId,
        [FromBody] DecisionRequest request,
        CancellationToken cancellationToken)
    {
        var app = await _db.LoanApplications
            .FirstOrDefaultAsync(x => x.ApplicationId == applicationId, cancellationToken);
        if (app is null) return NotFound();
        if (app.Status != ApplicationStatuses.ManualVerificationPending)
            return Conflict(new { detail = "Application is not in manual verification pending state." });

        var newStatus = request.Decision switch
        {
            "Approve" => ApplicationStatuses.ManualApproved,
            "Reject" => ApplicationStatuses.ManualRejected,
            _ => ""
        };
        var reasonCode = request.Decision switch
        {
            "Approve" => "ManualApprove",
            "Reject" => "ManualReject",
            _ => "ManualDecision"
        };
        if (string.IsNullOrEmpty(newStatus))
            return BadRequest(new { detail = "Decision must be one of: Approve, Reject." });

        if (request.Decision == "Approve" && request.ApprovedPrincipal is { } ap)
        {
            if (ap < app.ProductMinPrincipal || ap > app.RequestedPrincipal)
                return BadRequest(new
                {
                    detail = $"ApprovedPrincipal must be between {app.ProductMinPrincipal:F2} and {app.RequestedPrincipal:F2}."
                });
            app.ApprovedPrincipal = decimal.Round(ap, 2, MidpointRounding.AwayFromZero);
        }
        else if (request.Decision == "Approve")
            app.ApprovedPrincipal = null;
        else
            app.ApprovedPrincipal = null;

        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var now = DateTime.UtcNow;
        _db.ApplicationStatusHistories.Add(new ApplicationStatusHistory
        {
            HistoryId = Guid.NewGuid(),
            ApplicationId = app.ApplicationId,
            FromStatus = app.Status,
            ToStatus = newStatus,
            ChangedAtUtc = now,
            ChangedByUserId = userId,
            ReasonCode = string.IsNullOrWhiteSpace(request.Reason) ? reasonCode : request.Reason
        });
        app.Status = newStatus;
        app.UpdatedAtUtc = now;
        app.UpdatedByUserId = userId;
        await _db.SaveChangesAsync(cancellationToken);
        return NoContent();
    }
}
