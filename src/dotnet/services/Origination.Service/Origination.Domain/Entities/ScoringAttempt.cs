namespace Origination.Domain.Entities;

public class ScoringAttempt
{
    public Guid ScoringAttemptId { get; set; }
    public Guid ApplicationId { get; set; }
    public LoanApplication LoanApplication { get; set; } = null!;
    public string IdempotencyKey { get; set; } = "";
    public string Status { get; set; } = "Pending";
    public string? CorrelationId { get; set; }
    public string? CausationId { get; set; }
    public DateTime StartedAtUtc { get; set; }
    public DateTime? CompletedAtUtc { get; set; }

    public ScoringResult? Result { get; set; }
}

