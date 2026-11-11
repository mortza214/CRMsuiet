using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CrmSuite.BuildingBlocks.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Phase12CustomerNotification : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "preferred_notification_channel",
                table: "customers",
                type: "varchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "",
                collation: "utf8mb4_persian_ci");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "preferred_notification_channel",
                table: "customers");
        }
    }
}
