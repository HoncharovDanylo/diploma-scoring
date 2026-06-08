namespace Origination.Domain.Entities;

public class ApplicationStatusHistory
{
    public Guid HistoryId { get; set; }
    public Guid ApplicationId { get; set; }
    public LoanApplication LoanApplication { get; set; } = null!;
    public string? FromStatus { get; set; }
    public string ToStatus { get; set; } = "";
    public DateTime ChangedAtUtc { get; set; }
    public Guid? ChangedByUserId { get; set; }
    public string? ReasonCode { get; set; }
}
