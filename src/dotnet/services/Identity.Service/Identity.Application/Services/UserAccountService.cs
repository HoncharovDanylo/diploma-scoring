using Identity.Application.Abstractions;
using Identity.Application.Models;
using Identity.Domain;
using Identity.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Identity.Application.Services;

public sealed class UserAccountService : IUserAccountService
{
    private static readonly TimeZoneInfo UaTz = TimeZoneInfo.FindSystemTimeZoneById(
        OperatingSystem.IsWindows() ? "FLE Standard Time" : "Europe/Kyiv");

    private readonly UserManager<AppUser> _users;

    public UserAccountService(UserManager<AppUser> users) => _users = users;

    public async Task<RegisterResult> RegisterAsync(RegisterUserRequest request, CancellationToken cancellationToken)
    {
        if (request.EmploymentStatus is null ||
            !EmploymentCatalog.All.Contains(request.EmploymentStatus!))
        {
            return new RegisterResult
            {
                Succeeded = false,
                Errors = new[] { "EmploymentStatus is invalid. Use one of: " + string.Join(", ", EmploymentCatalog.All) }
            };
        }

        var today = DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, UaTz).Date);
        var age = today.Year - request.DateOfBirth.Year -
                  (today.DayOfYear < request.DateOfBirth.DayOfYear ? 1 : 0);
        if (age is < 18 or > 100)
        {
            return new RegisterResult
            {
                Succeeded = false,
                Errors = new[] { "DateOfBirth must imply age between 18 and 100." }
            };
        }

        if (request.MonthlyIncome is < 0)
        {
            return new RegisterResult { Succeeded = false, Errors = new[] { "MonthlyIncome cannot be negative." } };
        }

        if (request.MonthlyIncome is null or 0
            && (request.EmploymentStatus == EmploymentCatalog.Employed
                || request.EmploymentStatus == EmploymentCatalog.SelfEmployed))
        {
            return new RegisterResult
            {
                Succeeded = false,
                Errors = new[] { "MonthlyIncome is required for employed/self-employed applicants." }
            };
        }

        var user = new AppUser
        {
            Id = Guid.NewGuid(),
            UserName = request.Email,
            Email = request.Email,
            DisplayName = request.DisplayName,
            DateOfBirth = request.DateOfBirth,
            MonthlyIncome = request.MonthlyIncome,
            EmploymentStatus = request.EmploymentStatus,
            RegionCode = string.IsNullOrWhiteSpace(request.RegionCode) ? null : request.RegionCode.Trim(),
            TaxIdMasked = string.IsNullOrWhiteSpace(request.TaxIdMasked) ? null : request.TaxIdMasked.Trim(),
            PhoneNumber = string.IsNullOrWhiteSpace(request.PhoneNumber) ? null : request.PhoneNumber.Trim()
        };

        var result = await _users.CreateAsync(user, request.Password);
        if (!result.Succeeded)
            return new RegisterResult
            {
                Succeeded = false,
                Errors = result.Errors.Select(e => e.Description).ToList()
            };

        await _users.AddToRoleAsync(user, "Customer");
        return new RegisterResult { Succeeded = true, UserId = user.Id };
    }

    public async Task<LoginResult?> LoginAsync(LoginRequest request, CancellationToken cancellationToken)
    {
        var user = await _users.FindByEmailAsync(request.Email);
        if (user is null || !await _users.CheckPasswordAsync(user, request.Password))
            return null;
        var roles = await _users.GetRolesAsync(user);
        return new LoginResult { UserId = user.Id, Roles = roles.ToList() };
    }

    public async Task<UserPublicProfileDto?> GetPublicProfileAsync(Guid userId, CancellationToken cancellationToken)
    {
        var user = await _users.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);
        if (user is null) return null;
        return new UserPublicProfileDto
        {
            Id = user.Id,
            Email = user.Email,
            DisplayName = user.DisplayName,
            DateOfBirth = user.DateOfBirth,
            EmploymentStatus = user.EmploymentStatus,
            PhoneNumber = user.PhoneNumber,
            MonthlyIncome = user.MonthlyIncome
        };
    }

    public async Task<UserRiskProfileDto?> GetRiskProfileAsync(Guid userId, CancellationToken cancellationToken)
    {
        var user = await _users.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);
        if (user is null) return null;
        return new UserRiskProfileDto
        {
            UserId = user.Id,
            DisplayName = user.DisplayName,
            DateOfBirth = user.DateOfBirth,
            MonthlyIncome = user.MonthlyIncome,
            EmploymentStatus = user.EmploymentStatus,
            TaxIdMasked = user.TaxIdMasked
        };
    }
}
