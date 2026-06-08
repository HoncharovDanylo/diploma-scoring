using Microsoft.EntityFrameworkCore;
using Origination.Domain.Entities;

namespace Origination.Infrastructure.Persistence;

public class OriginationDbContext : DbContext
{
    public OriginationDbContext(DbContextOptions<OriginationDbContext> options) : base(options) { }

    public DbSet<Applicant> Applicants => Set<Applicant>();
    public DbSet<LoanApplication> LoanApplications => Set<LoanApplication>();
    public DbSet<LoanProduct> LoanProducts => Set<LoanProduct>();
    public DbSet<ApplicationStatusHistory> ApplicationStatusHistories => Set<ApplicationStatusHistory>();
    public DbSet<ScoringAttempt> ScoringAttempts => Set<ScoringAttempt>();
    public DbSet<ScoringResult> ScoringResults => Set<ScoringResult>();
    public DbSet<IntegrationOutboxMessage> IntegrationOutbox => Set<IntegrationOutboxMessage>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Applicant>(e =>
        {
            e.HasKey(x => x.ApplicantId);
            e.Property(x => x.FullName).HasMaxLength(300);
            e.Property(x => x.TaxIdMasked).HasMaxLength(32);
            e.Property(x => x.MonthlyIncome).HasPrecision(18, 2);
            e.Property(x => x.EmploymentStatus).HasMaxLength(64);
        });

        modelBuilder.Entity<LoanApplication>(e =>
        {
            e.HasKey(x => x.ApplicationId);
            e.Property(x => x.RequestedPrincipal).HasPrecision(18, 2);
            e.Property(x => x.ApprovedPrincipal).HasPrecision(18, 2);
            e.Property(x => x.AppliedInterestRatePerDay).HasPrecision(9, 6);
            e.Property(x => x.CalculatedRepaymentAmount).HasPrecision(18, 2);
            e.Property(x => x.ProductCode).HasMaxLength(64);
            e.Property(x => x.ProductMinPrincipal).HasPrecision(18, 2);
            e.Property(x => x.ProductMaxPrincipal).HasPrecision(18, 2);
            e.Property(x => x.Status).HasMaxLength(64);
            e.Property(x => x.Purpose).HasMaxLength(200);
            e.HasIndex(x => x.Status);
            e.HasOne(x => x.Applicant).WithMany(x => x.LoanApplications).HasForeignKey(x => x.ApplicantId);
        });

        modelBuilder.Entity<LoanProduct>(e =>
        {
            e.HasKey(x => x.LoanProductId);
            e.Property(x => x.ProductCode).HasMaxLength(64);
            e.Property(x => x.MinPrincipal).HasPrecision(18, 2);
            e.Property(x => x.MaxPrincipal).HasPrecision(18, 2);
            e.Property(x => x.InterestRatePerDay).HasPrecision(9, 6);
            e.HasIndex(x => new { x.ProductCode, x.Version }).IsUnique();
            e.HasIndex(x => x.IsActive);
        });

        modelBuilder.Entity<ApplicationStatusHistory>(e =>
        {
            e.HasKey(x => x.HistoryId);
            e.Property(x => x.FromStatus).HasMaxLength(64);
            e.Property(x => x.ToStatus).HasMaxLength(64);
            e.HasOne(x => x.LoanApplication).WithMany(x => x.StatusHistory).HasForeignKey(x => x.ApplicationId);
        });

        modelBuilder.Entity<ScoringAttempt>(e =>
        {
            e.HasKey(x => x.ScoringAttemptId);
            e.Property(x => x.IdempotencyKey).HasMaxLength(128);
            e.HasIndex(x => x.IdempotencyKey).IsUnique();
            e.Property(x => x.Status).HasMaxLength(32);
            e.HasOne(x => x.LoanApplication).WithMany(x => x.ScoringAttempts).HasForeignKey(x => x.ApplicationId);
            e.HasOne(x => x.Result).WithOne(x => x.ScoringAttempt).HasForeignKey<ScoringResult>(x => x.ScoringAttemptId);
        });

        modelBuilder.Entity<ScoringResult>(e =>
        {
            e.HasKey(x => x.ScoringResultId);
            e.Property(x => x.ProbabilityOfDefault).HasPrecision(9, 6);
            e.Property(x => x.FinalDecision).HasMaxLength(32);
            e.Property(x => x.ModelId).HasMaxLength(64);
            e.Property(x => x.ModelVersion).HasMaxLength(32);
        });

        modelBuilder.Entity<IntegrationOutboxMessage>(e =>
        {
            e.HasKey(x => x.OutboxId);
            e.Property(x => x.EventType).HasMaxLength(128);
            e.HasIndex(x => x.PublishedAtUtc);
        });
    }
}
