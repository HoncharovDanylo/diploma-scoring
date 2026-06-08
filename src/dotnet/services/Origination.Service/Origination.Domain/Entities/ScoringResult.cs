namespace Origination.Domain.Entities;

public class ScoringResult
{
    public Guid ScoringResultId { get; set; }
    public Guid ScoringAttemptId { get; set; }
    public ScoringAttempt ScoringAttempt { get; set; } = null!;
    public decimal ProbabilityOfDefault { get; set; }
    public string FinalDecision { get; set; } = "";
    public string ModelId { get; set; } = "";
    public string ModelVersion { get; set; } = "";
    public string ExplanationJson { get; set; } = "";
}
