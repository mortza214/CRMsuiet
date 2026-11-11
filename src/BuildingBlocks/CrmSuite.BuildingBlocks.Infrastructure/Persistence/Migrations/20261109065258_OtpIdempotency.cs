using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CrmSuite.BuildingBlocks.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class OtpIdempotency : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "correlation_id",
                table: "customer_otps",
                type: "varchar(100)",
                maxLength: 100,
                nullable: true,
                collation: "utf8mb4_persian_ci");

            migrationBuilder.CreateIndex(
                name: "ux_customer_otps_correlation_id",
                table: "customer_otps",
                column: "correlation_id",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ux_customer_otps_correlation_id",
                table: "customer_otps");

            migrationBuilder.DropColumn(
                name: "correlation_id",
                table: "customer_otps");
        }
    }
}
