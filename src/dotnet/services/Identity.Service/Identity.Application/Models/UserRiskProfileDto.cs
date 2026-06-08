namespace Identity.Application.Models;

public sealed class UserRiskProfileDto
{
    public Guid UserId { get; init; }
    public string? DisplayName { get; init; }
    public DateOnly? DateOfBirth { get; init; }
    public decimal? MonthlyIncome { get; init; }
    public string? EmploymentStatus { get; init; }
    public string? TaxIdMasked { get; init; }
}
