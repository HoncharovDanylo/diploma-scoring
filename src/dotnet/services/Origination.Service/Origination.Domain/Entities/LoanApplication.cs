namespace Origination.Domain.Entities;

public class LoanApplication
{
    public Guid ApplicationId { get; set; }
    public Guid ApplicantId { get; set; }
    public Applicant Applicant { get; set; } = null!;
    public decimal RequestedPrincipal { get; set; }
    public decimal? ApprovedPrincipal { get; set; }
    public int RequestedTermDays { get; set; }
    public decimal AppliedInterestRatePerDay { get; set; }
    public decimal CalculatedRepaymentAmount { get; set; }
    public string ProductCode { get; set; } = "";
    public int ProductVersion { get; set; }
    public decimal ProductMinPrincipal { get; set; }
    public decimal ProductMaxPrincipal { get; set; }
    public int ProductMinTermDays { get; set; }
    public int ProductMaxTermDays { get; set; }
    public string? Purpose { get; set; }
    public string Status { get; set; } = "";
    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
    public Guid? CreatedByUserId { get; set; }
    public Guid? UpdatedByUserId { get; set; }

    public ICollection<ApplicationStatusHistory> StatusHistory { get; set; } = new List<ApplicationStatusHistory>();
    public ICollection<ScoringAttempt> ScoringAttempts { get; set; } = new List<ScoringAttempt>();
}
