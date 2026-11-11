using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CrmSuite.BuildingBlocks.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Phase1514TaskActionOverride : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "action_key",
                table: "workflow_tasks",
                type: "varchar(40)",
                maxLength: 40,
                nullable: true,
                collation: "utf8mb4_persian_ci");

            migrationBuilder.AddColumn<string>(
                name: "form_code",
                table: "workflow_tasks",
                type: "varchar(100)",
                maxLength: 100,
                nullable: true,
                collation: "utf8mb4_persian_ci");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "action_key",
                table: "workflow_tasks");

            migrationBuilder.DropColumn(
                name: "form_code",
                table: "workflow_tasks");
        }
    }
}
