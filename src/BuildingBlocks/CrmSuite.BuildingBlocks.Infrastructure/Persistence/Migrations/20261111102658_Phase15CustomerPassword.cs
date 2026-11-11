using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CrmSuite.BuildingBlocks.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Phase15CustomerPassword : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "password_hash",
                table: "customers",
                type: "varchar(200)",
                maxLength: 200,
                nullable: true,
                collation: "utf8mb4_persian_ci");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "password_hash",
                table: "customers");
        }
    }
}
