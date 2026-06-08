namespace Lending.Contracts.Events;

public static class EventTypes
{
    public const string ScoringRequested = nameof(ScoringRequested);
    public const string ScoringCompleted = nameof(ScoringCompleted);
    public const string ScoringFailed = nameof(ScoringFailed);
    public const string PortfolioOptimizationRequested = nameof(PortfolioOptimizationRequested);
    public const string PortfolioOptimizationCompleted = nameof(PortfolioOptimizationCompleted);
    public const string PortfolioOptimizationFailed = nameof(PortfolioOptimizationFailed);
}
