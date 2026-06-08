using Microsoft.AspNetCore.Identity;

namespace Identity.Infrastructure.Persistence;

public sealed class AppUser : IdentityUser<Guid>
{
    public string? DisplayName { get; set; }

    public DateOnly? DateOfBirth { get; set; }

    public decimal? MonthlyIncome { get; set; }

    public string? EmploymentStatus { get; set; }

    public string? RegionCode { get; set; }

    public string? TaxIdMasked { get; set; }
}
