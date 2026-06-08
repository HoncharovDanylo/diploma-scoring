using Identity.Application.Models;

namespace Identity.Application.Abstractions;

public interface IUserAccountService
{
    Task<RegisterResult> RegisterAsync(RegisterUserRequest request, CancellationToken cancellationToken);

    Task<LoginResult?> LoginAsync(LoginRequest request, CancellationToken cancellationToken);

    Task<UserPublicProfileDto?> GetPublicProfileAsync(Guid userId, CancellationToken cancellationToken);

    Task<UserRiskProfileDto?> GetRiskProfileAsync(Guid userId, CancellationToken cancellationToken);
}
