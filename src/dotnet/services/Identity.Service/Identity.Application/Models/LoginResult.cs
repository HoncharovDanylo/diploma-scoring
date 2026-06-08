namespace Identity.Application.Models;

public sealed class LoginResult
{
    public required Guid UserId { get; init; }
    public required IReadOnlyList<string> Roles { get; init; }
}
