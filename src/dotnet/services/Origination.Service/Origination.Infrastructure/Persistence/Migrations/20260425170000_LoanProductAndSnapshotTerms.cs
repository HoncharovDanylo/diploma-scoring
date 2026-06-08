using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Origination.Infrastructure.Persistence.Migrations
{
    [DbContext(typeof(OriginationDbContext))]
    [Migration("20260425170000_LoanProductAndSnapshotTerms")]
    public partial class LoanProductAndSnapshotTerms : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "LoanProducts",
                columns: table => new
                {
                    LoanProductId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProductCode = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    Version = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    MinPrincipal = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    MaxPrincipal = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    MinTermDays = table.Column<int>(type: "int", nullable: false),
                    MaxTermDays = table.Column<int>(type: "int", nullable: false),
                    InterestRatePerDay = table.Column<decimal>(type: "decimal(9,6)", precision: 9, scale: 6, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LoanProducts", x => x.LoanProductId);
                });

            migrationBuilder.AddColumn<decimal>(
                name: "AppliedInterestRatePerDay",
                table: "LoanApplications",
                type: "decimal(9,6)",
                precision: 9,
                scale: 6,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "CalculatedRepaymentAmount",
                table: "LoanApplications",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "ProductCode",
                table: "LoanApplications",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "ProductMaxTermDays",
                table: "LoanApplications",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<decimal>(
                name: "ProductMaxPrincipal",
                table: "LoanApplications",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<int>(
                name: "ProductMinTermDays",
                table: "LoanApplications",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<decimal>(
                name: "ProductMinPrincipal",
                table: "LoanApplications",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<int>(
                name: "ProductVersion",
                table: "LoanApplications",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.Sql(
                """
                INSERT INTO LoanProducts (
                    LoanProductId, ProductCode, Version, IsActive,
                    MinPrincipal, MaxPrincipal, MinTermDays, MaxTermDays, InterestRatePerDay,
                    CreatedAtUtc, UpdatedAtUtc, UpdatedByUserId
                )
                VALUES (
                    NEWID(), 'STD-LOAN', 1, 1,
                    1000.00, 200000.00, 7, 365, 0.001500,
                    SYSUTCDATETIME(), SYSUTCDATETIME(), NULL
                );
                """
            );

            migrationBuilder.Sql(
                """
                UPDATE LoanApplications
                SET
                    ProductCode = 'LEGACY',
                    ProductVersion = 0,
                    ProductMinPrincipal = RequestedPrincipal,
                    ProductMaxPrincipal = RequestedPrincipal,
                    ProductMinTermDays = RequestedTermDays,
                    ProductMaxTermDays = RequestedTermDays,
                    AppliedInterestRatePerDay = 0.000000,
                    CalculatedRepaymentAmount = RequestedPrincipal;
                """
            );

            migrationBuilder.CreateIndex(
                name: "IX_LoanProducts_IsActive",
                table: "LoanProducts",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_LoanProducts_ProductCode_Version",
                table: "LoanProducts",
                columns: new[] { "ProductCode", "Version" },
                unique: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "LoanProducts");

            migrationBuilder.DropColumn(name: "AppliedInterestRatePerDay", table: "LoanApplications");
            migrationBuilder.DropColumn(name: "CalculatedRepaymentAmount", table: "LoanApplications");
            migrationBuilder.DropColumn(name: "ProductCode", table: "LoanApplications");
            migrationBuilder.DropColumn(name: "ProductMaxTermDays", table: "LoanApplications");
            migrationBuilder.DropColumn(name: "ProductMaxPrincipal", table: "LoanApplications");
            migrationBuilder.DropColumn(name: "ProductMinTermDays", table: "LoanApplications");
            migrationBuilder.DropColumn(name: "ProductMinPrincipal", table: "LoanApplications");
            migrationBuilder.DropColumn(name: "ProductVersion", table: "LoanApplications");
        }
    }
}
