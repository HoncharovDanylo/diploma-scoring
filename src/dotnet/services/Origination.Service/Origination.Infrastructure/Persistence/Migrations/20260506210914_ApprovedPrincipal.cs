using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Origination.Infrastructure.Persistence.Migrations
{
    public partial class ApprovedPrincipal : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "ApprovedPrincipal",
                table: "LoanApplications",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ApprovedPrincipal",
                table: "LoanApplications");
        }
    }
}
