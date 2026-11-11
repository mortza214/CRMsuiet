using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CrmSuite.BuildingBlocks.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Phase7WorkflowActions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "action_key",
                table: "workflow_states",
                type: "varchar(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "None",
                collation: "utf8mb4_persian_ci");

            migrationBuilder.Sql("UPDATE workflow_states SET action_key = 'None' WHERE action_key = '';");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "action_key",
                table: "workflow_states");
        }
    }
}
