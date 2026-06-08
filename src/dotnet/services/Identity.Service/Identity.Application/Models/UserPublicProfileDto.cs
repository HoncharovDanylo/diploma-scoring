namespace Identity.Application.Models;

public sealed class UserPublicProfileDto
{
    public Guid Id { get; init; }
    public string? Email { get; init; }
    public string? DisplayName { get; init; }
    public DateOnly? DateOfBirth { get; init; }
    public string? EmploymentStatus { get; init; }

    public string? PhoneNumber { get; init; }

    public decimal? MonthlyIncome { get; init; }
}
