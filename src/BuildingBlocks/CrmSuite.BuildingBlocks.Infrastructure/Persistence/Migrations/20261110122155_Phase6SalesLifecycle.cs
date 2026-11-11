using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CrmSuite.BuildingBlocks.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Phase6SalesLifecycle : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "payment_mode",
                table: "invoices",
                type: "varchar(30)",
                maxLength: 30,
                nullable: true,
                collation: "utf8mb4_persian_ci");

            migrationBuilder.AddColumn<bool>(
                name: "requires_payment",
                table: "invoices",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "payment_mode",
                table: "invoices");

            migrationBuilder.DropColumn(
                name: "requires_payment",
                table: "invoices");
        }
    }
}
