using System.Text.Json.Serialization;

namespace Lending.Contracts.Events;

public sealed class ScoringRequestedPayload
{
    [JsonPropertyName("applicationId")]
    public Guid ApplicationId { get; set; }

    [JsonPropertyName("scoringAttemptId")]
    public Guid ScoringAttemptId { get; set; }

    [JsonPropertyName("requestedAtUtc")]
    public DateTimeOffset RequestedAtUtc { get; set; }
}

public sealed class ScoringCompletedPayload
{
    [JsonPropertyName("scoringAttemptId")]
    public Guid ScoringAttemptId { get; set; }

    [JsonPropertyName("applicationId")]
    public Guid ApplicationId { get; set; }

    [JsonPropertyName("probabilityOfDefault")]
    public string ProbabilityOfDefault { get; set; } = "";

    [JsonPropertyName("finalDecision")]
    public string FinalDecision { get; set; } = "";

    [JsonPropertyName("modelId")]
    public string ModelId { get; set; } = "";

    [JsonPropertyName("modelVersion")]
    public string ModelVersion { get; set; } = "";

    [JsonPropertyName("topFactors")]
    public List<ExplanationFactorDto> TopFactors { get; set; } = [];
}

public sealed class ExplanationFactorDto
{
    [JsonPropertyName("featureName")]
    public string FeatureName { get; set; } = "";

    [JsonPropertyName("contribution")]
    public string Contribution { get; set; } = "";

    [JsonPropertyName("direction")]
    public string? Direction { get; set; }
}

public sealed class ScoringFailedPayload
{
    [JsonPropertyName("scoringAttemptId")]
    public Guid ScoringAttemptId { get; set; }

    [JsonPropertyName("applicationId")]
    public Guid ApplicationId { get; set; }

    [JsonPropertyName("errorCode")]
    public string ErrorCode { get; set; } = "";

    [JsonPropertyName("retryable")]
    public bool Retryable { get; set; }

    [JsonPropertyName("message")]
    public string Message { get; set; } = "";
}
