using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace CrmSuite.BuildingBlocks.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class FoundationInitial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "audit_logs",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    entity_name = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: false, collation: "utf8mb4_persian_ci"),
                    entity_id = table.Column<string>(type: "varchar(128)", maxLength: 128, nullable: false, collation: "utf8mb4_persian_ci"),
                    action = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false, collation: "utf8mb4_persian_ci"),
                    old_values = table.Column<string>(type: "longtext", nullable: true, collation: "utf8mb4_persian_ci"),
                    new_values = table.Column<string>(type: "longtext", nullable: true, collation: "utf8mb4_persian_ci"),
                    user_id = table.Column<Guid>(type: "char(36)", nullable: true, collation: "ascii_general_ci"),
                    user_ip = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: true, collation: "utf8mb4_persian_ci"),
                    timestamp = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_audit_logs", x => x.id);
                })
                .Annotation("Relational:Collation", "utf8mb4_persian_ci");

            migrationBuilder.CreateTable(
                name: "sequences",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    name = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false, collation: "utf8mb4_persian_ci"),
                    prefix = table.Column<string>(type: "varchar(30)", maxLength: 30, nullable: false, collation: "utf8mb4_persian_ci"),
                    current_value = table.Column<long>(type: "bigint", nullable: false),
                    format = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: false, collation: "utf8mb4_persian_ci"),
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
                    table.PrimaryKey("PK_sequences", x => x.id);
                })
                .Annotation("Relational:Collation", "utf8mb4_persian_ci");

            migrationBuilder.CreateTable(
                name: "settings",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    key = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: false, collation: "utf8mb4_persian_ci"),
                    value = table.Column<string>(type: "longtext", nullable: false, collation: "utf8mb4_persian_ci"),
                    group_name = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false, collation: "utf8mb4_persian_ci"),
                    is_secret = table.Column<bool>(type: "tinyint(1)", nullable: false),
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
                    table.PrimaryKey("PK_settings", x => x.id);
                })
                .Annotation("Relational:Collation", "utf8mb4_persian_ci");

            migrationBuilder.InsertData(
                table: "sequences",
                columns: new[] { "id", "created_at", "created_by_user_id", "current_value", "deleted_at", "deleted_by_user_id", "format", "is_deleted", "name", "prefix", "updated_at", "updated_by_user_id" },
                values: new object[,]
                {
                    { new Guid("11111111-1111-1111-1111-111111111111"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, 0L, null, null, "{Prefix}{Year}{CurrentValue:D5}", false, "InvoiceNumber", "INV-", null, null },
                    { new Guid("22222222-2222-2222-2222-222222222222"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, 0L, null, null, "{Prefix}{Year}{CurrentValue:D5}", false, "ContractNumber", "CTR-", null, null },
                    { new Guid("33333333-3333-3333-3333-333333333333"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, 0L, null, null, "{Prefix}{Year}{CurrentValue:D5}", false, "OrderNumber", "ORD-", null, null }
                });

            migrationBuilder.CreateIndex(
                name: "IX_audit_logs_entity_name_entity_id",
                table: "audit_logs",
                columns: new[] { "entity_name", "entity_id" });

            migrationBuilder.CreateIndex(
                name: "IX_audit_logs_timestamp",
                table: "audit_logs",
                column: "timestamp");

            migrationBuilder.CreateIndex(
                name: "IX_audit_logs_user_id",
                table: "audit_logs",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "IX_sequences_is_deleted",
                table: "sequences",
                column: "is_deleted");

            migrationBuilder.CreateIndex(
                name: "ux_sequences_name",
                table: "sequences",
                column: "name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_settings_group_name",
                table: "settings",
                column: "group_name");

            migrationBuilder.CreateIndex(
                name: "IX_settings_is_deleted",
                table: "settings",
                column: "is_deleted");

            migrationBuilder.CreateIndex(
                name: "ux_settings_key",
                table: "settings",
                column: "key",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "audit_logs");

            migrationBuilder.DropTable(
                name: "sequences");

            migrationBuilder.DropTable(
                name: "settings");
        }
    }
}
