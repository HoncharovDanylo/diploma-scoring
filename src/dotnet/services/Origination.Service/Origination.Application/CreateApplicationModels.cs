namespace Origination.Application;

public sealed class CreateApplicationRequestDto
{
    public decimal RequestedPrincipal { get; set; }
    public int RequestedTermDays { get; set; }
    public string? Purpose { get; set; }
}

public sealed class CreateApplicationResponseDto
{
    public Guid ApplicationId { get; set; }
    public Guid ScoringAttemptId { get; set; }
    public string CorrelationId { get; set; } = "";
}

public interface ILoanOriginationService
{
    Task<CreateApplicationResponseDto> CreateApplicationAsync(
        CreateApplicationRequestDto request,
        Guid userId,
        string correlationId,
        CancellationToken cancellationToken);
}
