using Lending.Contracts.Events;
using Microsoft.EntityFrameworkCore;
using Portfolio.Application;
using Portfolio.Domain;

namespace Portfolio.Infrastructure;

public sealed class PortfolioRunService : IPortfolioRunsService
{
    private readonly PortfolioDbContext _db;
    private readonly OriginationReadClient _origination;

    public PortfolioRunService(PortfolioDbContext db, OriginationReadClient origination)
    {
        _db = db;
        _origination = origination;
    }

    public async Task<Guid> StartRunAsync(DateOnly businessDate, Guid userId, string correlationId, CancellationToken cancellationToken)
    {
        var policy = await _db.DailyBudgetPolicies.FirstOrDefaultAsync(x => x.BusinessDate == businessDate, cancellationToken);
        if (policy is null)
            throw new InvalidOperationException($"No daily budget policy for {businessDate:yyyy-MM-dd}.");

        var alreadyIssuedToday = await _origination.GetIssuedPrincipalAsync(businessDate, cancellationToken);

        var availableBudget = policy.BudgetCap - alreadyIssuedToday;
        if (availableBudget <= 0m)
            throw new InvalidOperationException(
                $"No available budget left for {businessDate:yyyy-MM-dd}. " +
                $"Daily cap {policy.BudgetCap:F2}, already issued {alreadyIssuedToday:F2}.");

        var runId = Guid.NewGuid();
        var now = DateTime.UtcNow;
        var run = new PortfolioOptimizationRun
        {
            PortfolioRunId = runId,
            BusinessDate = businessDate,
            BudgetCapSnapshot = availableBudget,
            Status = "Pending",
            CreatedAtUtc = now,
            UpdatedAtUtc = now,
            CreatedByUserId = userId
        };

        var payload = new PortfolioOptimizationRequestedPayload
        {
            PortfolioRunId = runId,
            BusinessDate = businessDate.ToString("yyyy-MM-dd"),
            BudgetCap = availableBudget.ToString("F2", System.Globalization.CultureInfo.InvariantCulture)
        };

        var envelope = IntegrationEnvelope.Create(
            EventTypes.PortfolioOptimizationRequested,
            "Portfolio",
            1,
            payload,
            correlationId);

        var outbox = new PortfolioOutboxMessage
        {
            EventType = EventTypes.PortfolioOptimizationRequested,
            EnvelopeJson = envelope.SerializeToJson(),
            CreatedAtUtc = now
        };

        await using var tx = await _db.Database.BeginTransactionAsync(cancellationToken);
        _db.PortfolioOptimizationRuns.Add(run);
        _db.PortfolioIntegrationOutbox.Add(outbox);
        await _db.SaveChangesAsync(cancellationToken);
        await tx.CommitAsync(cancellationToken);

        return runId;
    }
}
