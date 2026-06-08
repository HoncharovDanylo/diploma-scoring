using System.Globalization;
using System.Text.Json;
using Lending.Contracts.Events;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Portfolio.Domain;

namespace Portfolio.Infrastructure;

public sealed class PortfolioOptimizationReplyHandler
{
    private readonly PortfolioDbContext _db;
    private readonly ILogger<PortfolioOptimizationReplyHandler> _logger;

    public PortfolioOptimizationReplyHandler(PortfolioDbContext db, ILogger<PortfolioOptimizationReplyHandler> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task HandleAsync(string envelopeJson, CancellationToken cancellationToken)
    {
        var envelope = IntegrationEnvelope.Deserialize(envelopeJson);
        if (envelope is null) return;

        var opts = IntegrationEnvelope.GetSerializerOptions();
        switch (envelope.EventType)
        {
            case EventTypes.PortfolioOptimizationCompleted:
                var ok = JsonSerializer.Deserialize<PortfolioOptimizationCompletedPayload>(envelope.Payload.GetRawText(), opts);
                if (ok is not null)
                    await ApplyCompletedAsync(ok, cancellationToken);
                break;
            case EventTypes.PortfolioOptimizationFailed:
                var fail = JsonSerializer.Deserialize<PortfolioOptimizationFailedPayload>(envelope.Payload.GetRawText(), opts);
                if (fail is not null)
                    await ApplyFailedAsync(fail, cancellationToken);
                break;
        }
    }

    private async Task ApplyCompletedAsync(PortfolioOptimizationCompletedPayload p, CancellationToken ct)
    {
        var run = await _db.PortfolioOptimizationRuns
            .Include(x => x.Selections)
            .FirstOrDefaultAsync(x => x.PortfolioRunId == p.PortfolioRunId, ct);

        if (run is null)
        {
            _logger.LogWarning("Portfolio run {Id} not found", p.PortfolioRunId);
            return;
        }

        if (run.Status == "Succeeded")
            return;

        var now = DateTime.UtcNow;
        run.Status = "Succeeded";
        run.UpdatedAtUtc = now;
        run.ObjectiveValue = decimal.TryParse(p.ObjectiveValue, NumberStyles.Any, CultureInfo.InvariantCulture, out var ov) ? ov : null;
        run.UsedBudget = decimal.TryParse(p.UsedBudget, NumberStyles.Any, CultureInfo.InvariantCulture, out var ub) ? ub : null;
        run.ExpectedPortfolioProfit = decimal.TryParse(p.ExpectedPortfolioProfit, NumberStyles.Any, CultureInfo.InvariantCulture, out var ep)
            ? ep
            : null;

        var rows = p.Selections;
        if (rows is { Count: > 0 })
        {
            var rank = 1;
            foreach (var s in rows)
            {
                var principal = decimal.TryParse(s.Principal, NumberStyles.Any, CultureInfo.InvariantCulture, out var pr) ? pr : 0m;
                var profit = decimal.TryParse(s.ExpectedProfit, NumberStyles.Any, CultureInfo.InvariantCulture, out var pf) ? pf : 0m;
                decimal? pd = decimal.TryParse(s.ProbabilityOfDefault ?? "0", NumberStyles.Any, CultureInfo.InvariantCulture, out var pdv)
                    ? pdv
                    : null;
                _db.PortfolioSelections.Add(new PortfolioSelection
                {
                    PortfolioRunId = run.PortfolioRunId,
                    ApplicationId = s.ApplicationId,
                    SelectedPrincipal = principal,
                    ExpectedProfitSnapshot = profit,
                    ProbabilityOfDefaultSnapshot = pd,
                    RankInSolution = rank++
                });
            }
        }
        else
        {
            var rank = 1;
            foreach (var id in p.SelectedApplicationIds)
            {
                _db.PortfolioSelections.Add(new PortfolioSelection
                {
                    PortfolioRunId = run.PortfolioRunId,
                    ApplicationId = id,
                    SelectedPrincipal = 0m,
                    ExpectedProfitSnapshot = 0m,
                    RankInSolution = rank++
                });
            }
        }

        await _db.SaveChangesAsync(ct);
    }

    private async Task ApplyFailedAsync(PortfolioOptimizationFailedPayload p, CancellationToken ct)
    {
        var run = await _db.PortfolioOptimizationRuns.FirstOrDefaultAsync(x => x.PortfolioRunId == p.PortfolioRunId, ct);
        if (run is null) return;
        run.Status = "Failed";
        run.UpdatedAtUtc = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);
    }
}
