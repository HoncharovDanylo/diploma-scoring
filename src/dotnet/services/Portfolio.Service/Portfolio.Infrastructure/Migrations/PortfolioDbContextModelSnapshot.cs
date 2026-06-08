using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Portfolio.Infrastructure;

#nullable disable

namespace Portfolio.Infrastructure.Migrations
{
    [DbContext(typeof(PortfolioDbContext))]
    partial class PortfolioDbContextModelSnapshot : ModelSnapshot
    {
        protected override void BuildModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "8.0.11")
                .HasAnnotation("Relational:MaxIdentifierLength", 128);

            SqlServerModelBuilderExtensions.UseIdentityColumns(modelBuilder);

            modelBuilder.Entity("Portfolio.Domain.DailyBudgetPolicy", b =>
                {
                    b.Property<DateOnly>("BusinessDate")
                        .HasColumnType("date");

                    b.Property<decimal>("BudgetCap")
                        .HasPrecision(18, 2)
                        .HasColumnType("decimal(18,2)");

                    b.Property<DateTime>("CreatedAtUtc")
                        .HasColumnType("datetime2");

                    b.Property<DateTime>("UpdatedAtUtc")
                        .HasColumnType("datetime2");

                    b.Property<Guid?>("UpdatedByUserId")
                        .HasColumnType("uniqueidentifier");

                    b.HasKey("BusinessDate");

                    b.ToTable("DailyBudgetPolicies");
                });

            modelBuilder.Entity("Portfolio.Domain.PortfolioOptimizationRun", b =>
                {
                    b.Property<Guid>("PortfolioRunId")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uniqueidentifier");

                    b.Property<decimal>("BudgetCapSnapshot")
                        .HasPrecision(18, 2)
                        .HasColumnType("decimal(18,2)");

                    b.Property<DateOnly>("BusinessDate")
                        .HasColumnType("date");

                    b.Property<DateTime>("CreatedAtUtc")
                        .HasColumnType("datetime2");

                    b.Property<Guid?>("CreatedByUserId")
                        .HasColumnType("uniqueidentifier");

                    b.Property<decimal?>("ExpectedPortfolioProfit")
                        .HasPrecision(18, 2)
                        .HasColumnType("decimal(18,2)");

                    b.Property<decimal?>("ObjectiveValue")
                        .HasPrecision(18, 4)
                        .HasColumnType("decimal(18,4)");

                    b.Property<string>("Status")
                        .IsRequired()
                        .HasMaxLength(32)
                        .HasColumnType("nvarchar(32)");

                    b.Property<DateTime>("UpdatedAtUtc")
                        .HasColumnType("datetime2");

                    b.Property<decimal?>("UsedBudget")
                        .HasPrecision(18, 2)
                        .HasColumnType("decimal(18,2)");

                    b.HasKey("PortfolioRunId");

                    b.ToTable("PortfolioOptimizationRuns");
                });

            modelBuilder.Entity("Portfolio.Domain.PortfolioOutboxMessage", b =>
                {
                    b.Property<long>("OutboxId")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("bigint");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<long>("OutboxId"));

                    b.Property<DateTime>("CreatedAtUtc")
                        .HasColumnType("datetime2");

                    b.Property<string>("EnvelopeJson")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("EventType")
                        .IsRequired()
                        .HasMaxLength(128)
                        .HasColumnType("nvarchar(128)");

                    b.Property<DateTime?>("PublishedAtUtc")
                        .HasColumnType("datetime2");

                    b.HasKey("OutboxId");

                    b.ToTable("PortfolioIntegrationOutbox");
                });

            modelBuilder.Entity("Portfolio.Domain.PortfolioSelection", b =>
                {
                    b.Property<Guid>("PortfolioRunId")
                        .HasColumnType("uniqueidentifier");

                    b.Property<Guid>("ApplicationId")
                        .HasColumnType("uniqueidentifier");

                    b.Property<decimal>("ExpectedProfitSnapshot")
                        .HasPrecision(18, 2)
                        .HasColumnType("decimal(18,2)");

                    b.Property<decimal?>("ProbabilityOfDefaultSnapshot")
                        .HasPrecision(9, 6)
                        .HasColumnType("decimal(9,6)");

                    b.Property<int>("RankInSolution")
                        .HasColumnType("int");

                    b.Property<decimal>("SelectedPrincipal")
                        .HasPrecision(18, 2)
                        .HasColumnType("decimal(18,2)");

                    b.HasKey("PortfolioRunId", "ApplicationId");

                    b.ToTable("PortfolioSelections");
                });

            modelBuilder.Entity("Portfolio.Domain.PortfolioSelection", b =>
                {
                    b.HasOne("Portfolio.Domain.PortfolioOptimizationRun", "Run")
                        .WithMany("Selections")
                        .HasForeignKey("PortfolioRunId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Run");
                });

            modelBuilder.Entity("Portfolio.Domain.PortfolioOptimizationRun", b =>
                {
                    b.Navigation("Selections");
                });
#pragma warning restore 612, 618
        }
    }
}
