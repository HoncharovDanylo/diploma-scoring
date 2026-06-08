using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Origination.Infrastructure.Persistence;

#nullable disable

namespace Origination.Infrastructure.Persistence.Migrations
{
    [DbContext(typeof(OriginationDbContext))]
    [Migration("20260424222609_ApplicantIdentitySnapshot")]
    partial class ApplicantIdentitySnapshot
    {
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "8.0.11")
                .HasAnnotation("Relational:MaxIdentifierLength", 128);

            SqlServerModelBuilderExtensions.UseIdentityColumns(modelBuilder);

            modelBuilder.Entity("Origination.Domain.Entities.Applicant", b =>
                {
                    b.Property<Guid>("ApplicantId")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uniqueidentifier");

                    b.Property<DateTime>("CreatedAtUtc")
                        .HasColumnType("datetime2");

                    b.Property<Guid?>("CreatedByUserId")
                        .HasColumnType("uniqueidentifier");

                    b.Property<DateOnly?>("DateOfBirth")
                        .HasColumnType("date");

                    b.Property<string>("EmploymentStatus")
                        .HasMaxLength(64)
                        .HasColumnType("nvarchar(64)");

                    b.Property<string>("FullName")
                        .IsRequired()
                        .HasMaxLength(300)
                        .HasColumnType("nvarchar(300)");

                    b.Property<decimal?>("MonthlyIncome")
                        .HasPrecision(18, 2)
                        .HasColumnType("decimal(18,2)");

                    b.Property<string>("RegionCode")
                        .HasMaxLength(16)
                        .HasColumnType("nvarchar(16)");

                    b.Property<string>("TaxIdMasked")
                        .HasMaxLength(32)
                        .HasColumnType("nvarchar(32)");

                    b.Property<DateTime>("UpdatedAtUtc")
                        .HasColumnType("datetime2");

                    b.Property<Guid?>("UpdatedByUserId")
                        .HasColumnType("uniqueidentifier");

                    b.Property<Guid?>("UserId")
                        .HasColumnType("uniqueidentifier");

                    b.HasKey("ApplicantId");

                    b.ToTable("Applicants");
                });

            modelBuilder.Entity("Origination.Domain.Entities.ApplicationStatusHistory", b =>
                {
                    b.Property<Guid>("HistoryId")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uniqueidentifier");

                    b.Property<Guid>("ApplicationId")
                        .HasColumnType("uniqueidentifier");

                    b.Property<DateTime>("ChangedAtUtc")
                        .HasColumnType("datetime2");

                    b.Property<Guid?>("ChangedByUserId")
                        .HasColumnType("uniqueidentifier");

                    b.Property<string>("FromStatus")
                        .HasMaxLength(64)
                        .HasColumnType("nvarchar(64)");

                    b.Property<string>("ReasonCode")
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("ToStatus")
                        .IsRequired()
                        .HasMaxLength(64)
                        .HasColumnType("nvarchar(64)");

                    b.HasKey("HistoryId");

                    b.HasIndex("ApplicationId");

                    b.ToTable("ApplicationStatusHistories");
                });

            modelBuilder.Entity("Origination.Domain.Entities.IntegrationOutboxMessage", b =>
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

                    b.HasIndex("PublishedAtUtc");

                    b.ToTable("IntegrationOutbox");
                });

            modelBuilder.Entity("Origination.Domain.Entities.LoanApplication", b =>
                {
                    b.Property<Guid>("ApplicationId")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uniqueidentifier");

                    b.Property<Guid>("ApplicantId")
                        .HasColumnType("uniqueidentifier");

                    b.Property<DateTime>("CreatedAtUtc")
                        .HasColumnType("datetime2");

                    b.Property<Guid?>("CreatedByUserId")
                        .HasColumnType("uniqueidentifier");

                    b.Property<string>("Purpose")
                        .HasMaxLength(200)
                        .HasColumnType("nvarchar(200)");

                    b.Property<decimal>("RequestedPrincipal")
                        .HasPrecision(18, 2)
                        .HasColumnType("decimal(18,2)");

                    b.Property<int>("RequestedTermMonths")
                        .HasColumnType("int");

                    b.Property<string>("Status")
                        .IsRequired()
                        .HasMaxLength(64)
                        .HasColumnType("nvarchar(64)");

                    b.Property<DateTime>("UpdatedAtUtc")
                        .HasColumnType("datetime2");

                    b.Property<Guid?>("UpdatedByUserId")
                        .HasColumnType("uniqueidentifier");

                    b.HasKey("ApplicationId");

                    b.HasIndex("ApplicantId");

                    b.HasIndex("Status");

                    b.ToTable("LoanApplications");
                });

            modelBuilder.Entity("Origination.Domain.Entities.ScoringAttempt", b =>
                {
                    b.Property<Guid>("ScoringAttemptId")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uniqueidentifier");

                    b.Property<Guid>("ApplicationId")
                        .HasColumnType("uniqueidentifier");

                    b.Property<string>("CausationId")
                        .HasColumnType("nvarchar(max)");

                    b.Property<DateTime?>("CompletedAtUtc")
                        .HasColumnType("datetime2");

                    b.Property<string>("CorrelationId")
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("IdempotencyKey")
                        .IsRequired()
                        .HasMaxLength(128)
                        .HasColumnType("nvarchar(128)");

                    b.Property<DateTime>("StartedAtUtc")
                        .HasColumnType("datetime2");

                    b.Property<string>("Status")
                        .IsRequired()
                        .HasMaxLength(32)
                        .HasColumnType("nvarchar(32)");

                    b.HasKey("ScoringAttemptId");

                    b.HasIndex("ApplicationId");

                    b.HasIndex("IdempotencyKey")
                        .IsUnique();

                    b.ToTable("ScoringAttempts");
                });

            modelBuilder.Entity("Origination.Domain.Entities.ScoringResult", b =>
                {
                    b.Property<Guid>("ScoringResultId")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uniqueidentifier");

                    b.Property<string>("ExplanationJson")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("FinalDecision")
                        .IsRequired()
                        .HasMaxLength(32)
                        .HasColumnType("nvarchar(32)");

                    b.Property<string>("ModelId")
                        .IsRequired()
                        .HasMaxLength(64)
                        .HasColumnType("nvarchar(64)");

                    b.Property<string>("ModelVersion")
                        .IsRequired()
                        .HasMaxLength(32)
                        .HasColumnType("nvarchar(32)");

                    b.Property<decimal>("ProbabilityOfDefault")
                        .HasPrecision(9, 6)
                        .HasColumnType("decimal(9,6)");

                    b.Property<Guid>("ScoringAttemptId")
                        .HasColumnType("uniqueidentifier");

                    b.HasKey("ScoringResultId");

                    b.HasIndex("ScoringAttemptId")
                        .IsUnique();

                    b.ToTable("ScoringResults");
                });

            modelBuilder.Entity("Origination.Domain.Entities.ApplicationStatusHistory", b =>
                {
                    b.HasOne("Origination.Domain.Entities.LoanApplication", "LoanApplication")
                        .WithMany("StatusHistory")
                        .HasForeignKey("ApplicationId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("LoanApplication");
                });

            modelBuilder.Entity("Origination.Domain.Entities.LoanApplication", b =>
                {
                    b.HasOne("Origination.Domain.Entities.Applicant", "Applicant")
                        .WithMany("LoanApplications")
                        .HasForeignKey("ApplicantId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Applicant");
                });

            modelBuilder.Entity("Origination.Domain.Entities.ScoringAttempt", b =>
                {
                    b.HasOne("Origination.Domain.Entities.LoanApplication", "LoanApplication")
                        .WithMany("ScoringAttempts")
                        .HasForeignKey("ApplicationId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("LoanApplication");
                });

            modelBuilder.Entity("Origination.Domain.Entities.ScoringResult", b =>
                {
                    b.HasOne("Origination.Domain.Entities.ScoringAttempt", "ScoringAttempt")
                        .WithOne("Result")
                        .HasForeignKey("Origination.Domain.Entities.ScoringResult", "ScoringAttemptId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("ScoringAttempt");
                });

            modelBuilder.Entity("Origination.Domain.Entities.Applicant", b =>
                {
                    b.Navigation("LoanApplications");
                });

            modelBuilder.Entity("Origination.Domain.Entities.LoanApplication", b =>
                {
                    b.Navigation("ScoringAttempts");

                    b.Navigation("StatusHistory");
                });

            modelBuilder.Entity("Origination.Domain.Entities.ScoringAttempt", b =>
                {
                    b.Navigation("Result");
                });
#pragma warning restore 612, 618
        }
    }
}
