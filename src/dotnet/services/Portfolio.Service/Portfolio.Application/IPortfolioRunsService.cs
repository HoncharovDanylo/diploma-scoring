namespace Portfolio.Application;

public interface IPortfolioRunsService
{
    Task<Guid> StartRunAsync(DateOnly businessDate, Guid userId, string correlationId, CancellationToken cancellationToken);
}
