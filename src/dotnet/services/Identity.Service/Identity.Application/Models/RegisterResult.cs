namespace Identity.Application.Models;

public sealed class RegisterResult
{
    public bool Succeeded { get; init; }
    public IReadOnlyList<string> Errors { get; init; } = Array.Empty<string>();
    public Guid? UserId { get; init; }
}
