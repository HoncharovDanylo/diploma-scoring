namespace Origination.Domain.Entities;

public class Applicant
{
    public Guid ApplicantId { get; set; }
    public Guid? UserId { get; set; }
    public string FullName { get; set; } = "";
    public string? TaxIdMasked { get; set; }

    public DateOnly? DateOfBirth { get; set; }

    public decimal? MonthlyIncome { get; set; }
    public string? EmploymentStatus { get; set; }
    public string? RegionCode { get; set; }

    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
    public Guid? CreatedByUserId { get; set; }
    public Guid? UpdatedByUserId { get; set; }

    public ICollection<LoanApplication> LoanApplications { get; set; } = new List<LoanApplication>();
}
