namespace Portfolio.Domain;

public sealed class DailyBudgetPolicy
{
    public DateOnly BusinessDate { get; set; }
    public decimal BudgetCap { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
    public Guid? UpdatedByUserId { get; set; }
}

public sealed class PortfolioOptimizationRun
{
    public Guid PortfolioRunId { get; set; }
    public DateOnly BusinessDate { get; set; }
    public decimal BudgetCapSnapshot { get; set; }
    public string Status { get; set; } = "Pending";
    public decimal? ObjectiveValue { get; set; }
    public decimal? UsedBudget { get; set; }
    public decimal? ExpectedPortfolioProfit { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
    public Guid? CreatedByUserId { get; set; }

    public ICollection<PortfolioSelection> Selections { get; set; } = new List<PortfolioSelection>();
}

public sealed class PortfolioSelection
{
    public Guid PortfolioRunId { get; set; }
    public PortfolioOptimizationRun Run { get; set; } = null!;
    public Guid ApplicationId { get; set; }
    public decimal SelectedPrincipal { get; set; }
    public decimal ExpectedProfitSnapshot { get; set; }
    public decimal? ProbabilityOfDefaultSnapshot { get; set; }
    public int RankInSolution { get; set; }
}

public sealed class PortfolioOutboxMessage
{
    public long OutboxId { get; set; }
    public string EventType { get; set; } = "";
    public string EnvelopeJson { get; set; } = "";
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? PublishedAtUtc { get; set; }
}
