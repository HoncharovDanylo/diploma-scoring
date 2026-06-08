using Microsoft.EntityFrameworkCore;
using Portfolio.Domain;

namespace Portfolio.Infrastructure;

public sealed class PortfolioDbContext : DbContext
{
    public PortfolioDbContext(DbContextOptions<PortfolioDbContext> options) : base(options) { }

    public DbSet<DailyBudgetPolicy> DailyBudgetPolicies => Set<DailyBudgetPolicy>();
    public DbSet<PortfolioOptimizationRun> PortfolioOptimizationRuns => Set<PortfolioOptimizationRun>();
    public DbSet<PortfolioSelection> PortfolioSelections => Set<PortfolioSelection>();
    public DbSet<PortfolioOutboxMessage> PortfolioIntegrationOutbox => Set<PortfolioOutboxMessage>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<DailyBudgetPolicy>(e =>
        {
            e.HasKey(x => x.BusinessDate);
            e.Property(x => x.BudgetCap).HasPrecision(18, 2);
        });

        modelBuilder.Entity<PortfolioOptimizationRun>(e =>
        {
            e.HasKey(x => x.PortfolioRunId);
            e.Property(x => x.BudgetCapSnapshot).HasPrecision(18, 2);
            e.Property(x => x.Status).HasMaxLength(32);
            e.Property(x => x.ObjectiveValue).HasPrecision(18, 4);
            e.Property(x => x.UsedBudget).HasPrecision(18, 2);
            e.Property(x => x.ExpectedPortfolioProfit).HasPrecision(18, 2);
            e.HasMany(x => x.Selections).WithOne(x => x.Run).HasForeignKey(x => x.PortfolioRunId);
        });

        modelBuilder.Entity<PortfolioSelection>(e =>
        {
            e.HasKey(x => new { x.PortfolioRunId, x.ApplicationId });
            e.Property(x => x.SelectedPrincipal).HasPrecision(18, 2);
            e.Property(x => x.ExpectedProfitSnapshot).HasPrecision(18, 2);
            e.Property(x => x.ProbabilityOfDefaultSnapshot).HasPrecision(9, 6);
        });

        modelBuilder.Entity<PortfolioOutboxMessage>(e =>
        {
            e.HasKey(x => x.OutboxId);
            e.Property(x => x.EventType).HasMaxLength(128);
        });
    }
}
