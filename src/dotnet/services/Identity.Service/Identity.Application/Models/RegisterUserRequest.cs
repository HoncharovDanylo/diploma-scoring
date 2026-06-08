using System.ComponentModel.DataAnnotations;

namespace Identity.Application.Models;

public sealed class RegisterUserRequest
{
    [Required, EmailAddress, MaxLength(256)]
    public string Email { get; set; } = "";

    [Required, MinLength(8), MaxLength(200)]
    public string Password { get; set; } = "";

    [MaxLength(200)]
    public string? DisplayName { get; set; }

    [Required]
    public DateOnly DateOfBirth { get; set; }

    [Range(0, 100_000_000)]
    public decimal? MonthlyIncome { get; set; }

    [Required, MaxLength(64)]
    public string? EmploymentStatus { get; set; }

    [MaxLength(16)]
    public string? RegionCode { get; set; }

    [MaxLength(32)]
    public string? TaxIdMasked { get; set; }

    [MaxLength(20)]
    public string? PhoneNumber { get; set; }
}
