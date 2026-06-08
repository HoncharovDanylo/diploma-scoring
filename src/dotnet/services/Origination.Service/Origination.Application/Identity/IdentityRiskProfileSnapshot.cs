namespace Origination.Application.Identity;

public sealed class IdentityRiskProfileSnapshot
{
    public Guid UserId { get; set; }
    public string? DisplayName { get; set; }
    public DateOnly? DateOfBirth { get; set; }
    public decimal? MonthlyIncome { get; set; }
    public string? EmploymentStatus { get; set; }
    public string? TaxIdMasked { get; set; }
}
