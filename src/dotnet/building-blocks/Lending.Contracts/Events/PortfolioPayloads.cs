using System.Text.Json.Serialization;

namespace Lending.Contracts.Events;

public sealed class PortfolioOptimizationRequestedPayload
{
    [JsonPropertyName("portfolioRunId")]
    public Guid PortfolioRunId { get; set; }

    [JsonPropertyName("businessDate")]
    public string BusinessDate { get; set; } = "";

    [JsonPropertyName("budgetCap")]
    public string BudgetCap { get; set; } = "";
}

public sealed class PortfolioOptimizationCompletedPayload
{
    [JsonPropertyName("portfolioRunId")]
    public Guid PortfolioRunId { get; set; }

    [JsonPropertyName("objectiveValue")]
    public string ObjectiveValue { get; set; } = "";

    [JsonPropertyName("usedBudget")]
    public string UsedBudget { get; set; } = "";

    [JsonPropertyName("expectedPortfolioProfit")]
    public string ExpectedPortfolioProfit { get; set; } = "";

    [JsonPropertyName("selectedApplicationIds")]
    public List<Guid> SelectedApplicationIds { get; set; } = [];

    [JsonPropertyName("selections")]
    public List<PortfolioSelectionItemDto>? Selections { get; set; }
}

public sealed class PortfolioSelectionItemDto
{
    [JsonPropertyName("applicationId")]
    public Guid ApplicationId { get; set; }

    [JsonPropertyName("principal")]
    public string Principal { get; set; } = "";

    [JsonPropertyName("expectedProfit")]
    public string ExpectedProfit { get; set; } = "";

    [JsonPropertyName("probabilityOfDefault")]
    public string? ProbabilityOfDefault { get; set; }
}

public sealed class PortfolioOptimizationFailedPayload
{
    [JsonPropertyName("portfolioRunId")]
    public Guid PortfolioRunId { get; set; }

    [JsonPropertyName("errorCode")]
    public string ErrorCode { get; set; } = "";

    [JsonPropertyName("retryable")]
    public bool Retryable { get; set; }

    [JsonPropertyName("message")]
    public string Message { get; set; } = "";
}
