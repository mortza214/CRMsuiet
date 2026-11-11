using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CrmSuite.BuildingBlocks.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Phase82LifecycleFlags : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "is_equipment_consigned",
                table: "orders",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "is_equipment_consigned",
                table: "customer_services",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "is_equipment_consigned",
                table: "orders");

            migrationBuilder.DropColumn(
                name: "is_equipment_consigned",
                table: "customer_services");
        }
    }
}
