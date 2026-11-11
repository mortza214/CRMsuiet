using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CrmSuite.BuildingBlocks.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Phase8CommissionEngine : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "owner_type",
                table: "customers",
                type: "varchar(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "CurrentUser",
                collation: "utf8mb4_persian_ci");

            migrationBuilder.Sql("UPDATE customers SET owner_type = 'CurrentUser' WHERE owner_type = '';");

            migrationBuilder.AddColumn<decimal>(
                name: "commission_base",
                table: "commission_entries",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<Guid>(
                name: "customer_id",
                table: "commission_entries",
                type: "char(36)",
                nullable: true,
                collation: "ascii_general_ci");

            migrationBuilder.AddColumn<string>(
                name: "customer_name",
                table: "commission_entries",
                type: "varchar(200)",
                maxLength: 200,
                nullable: true,
                collation: "utf8mb4_persian_ci");

            migrationBuilder.AddColumn<Guid>(
                name: "customer_service_id",
                table: "commission_entries",
                type: "char(36)",
                nullable: true,
                collation: "ascii_general_ci");

            migrationBuilder.AddColumn<DateTime>(
                name: "last_paid_at",
                table: "commission_entries",
                type: "datetime(6)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "paid_installments",
                table: "commission_entries",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "service_name",
                table: "commission_entries",
                type: "varchar(200)",
                maxLength: 200,
                nullable: true,
                collation: "utf8mb4_persian_ci");

            migrationBuilder.CreateTable(
                name: "commission_shares",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    commission_entry_id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    user_id = table.Column<Guid>(type: "char(36)", nullable: true, collation: "ascii_general_ci"),
                    percentage = table.Column<decimal>(type: "decimal(8,2)", precision: 8, scale: 2, nullable: false),
                    amount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    is_company_share = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    created_at = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    updated_at = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    created_by_user_id = table.Column<Guid>(type: "char(36)", nullable: true, collation: "ascii_general_ci"),
                    updated_by_user_id = table.Column<Guid>(type: "char(36)", nullable: true, collation: "ascii_general_ci"),
                    is_deleted = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    deleted_at = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    deleted_by_user_id = table.Column<Guid>(type: "char(36)", nullable: true, collation: "ascii_general_ci")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_commission_shares", x => x.id);
                    table.ForeignKey(
                        name: "FK_commission_shares_commission_entries_commission_entry_id",
                        column: x => x.commission_entry_id,
                        principalTable: "commission_entries",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("Relational:Collation", "utf8mb4_persian_ci");

            migrationBuilder.CreateIndex(
                name: "ix_commission_shares_entry",
                table: "commission_shares",
                column: "commission_entry_id");

            migrationBuilder.CreateIndex(
                name: "IX_commission_shares_is_deleted",
                table: "commission_shares",
                column: "is_deleted");

            migrationBuilder.CreateIndex(
                name: "ix_commission_shares_user",
                table: "commission_shares",
                column: "user_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "commission_shares");

            migrationBuilder.DropColumn(
                name: "owner_type",
                table: "customers");

            migrationBuilder.DropColumn(
                name: "commission_base",
                table: "commission_entries");

            migrationBuilder.DropColumn(
                name: "customer_id",
                table: "commission_entries");

            migrationBuilder.DropColumn(
                name: "customer_name",
                table: "commission_entries");

            migrationBuilder.DropColumn(
                name: "customer_service_id",
                table: "commission_entries");

            migrationBuilder.DropColumn(
                name: "last_paid_at",
                table: "commission_entries");

            migrationBuilder.DropColumn(
                name: "paid_installments",
                table: "commission_entries");

            migrationBuilder.DropColumn(
                name: "service_name",
                table: "commission_entries");
        }
    }
}
