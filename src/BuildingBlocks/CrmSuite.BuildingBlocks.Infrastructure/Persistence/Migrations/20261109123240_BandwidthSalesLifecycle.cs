using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CrmSuite.BuildingBlocks.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class BandwidthSalesLifecycle : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ux_invoices_order_type",
                table: "invoices");

            migrationBuilder.AddColumn<DateTime>(
                name: "activated_at",
                table: "orders",
                type: "datetime(6)",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "activated_by",
                table: "orders",
                type: "char(36)",
                nullable: true,
                collation: "ascii_general_ci");

            migrationBuilder.CreateTable(
                name: "customer_services",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    customer_id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    service_id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    order_id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    order_item_id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    service_name = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: false, collation: "utf8mb4_persian_ci"),
                    unit_price_snapshot = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    quantity = table.Column<int>(type: "int", nullable: false),
                    addons_total = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    attributes_json = table.Column<string>(type: "longtext", nullable: true, collation: "utf8mb4_persian_ci"),
                    commission_base = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    commission_percent = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: true),
                    status = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false, collation: "utf8mb4_persian_ci"),
                    activated_at = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    next_invoice_at = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    last_invoiced_at = table.Column<DateTime>(type: "datetime(6)", nullable: true),
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
                    table.PrimaryKey("PK_customer_services", x => x.id);
                })
                .Annotation("Relational:Collation", "utf8mb4_persian_ci");

            migrationBuilder.InsertData(
                table: "notification_templates",
                columns: new[] { "id", "body", "code", "created_at", "created_by_user_id", "deleted_at", "deleted_by_user_id", "is_deleted", "title", "updated_at", "updated_by_user_id" },
                values: new object[] { new Guid("63333333-3333-3333-3333-333333333333"), "فاکتور {InvoiceNumber} به مبلغ {Amount} تومان برای {CustomerName} صادر شد.", "InvoiceIssued", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, null, null, false, "فاکتور جدید", null, null });

            migrationBuilder.CreateIndex(
                name: "ix_invoices_order_type",
                table: "invoices",
                columns: new[] { "order_id", "type" });

            migrationBuilder.CreateIndex(
                name: "ix_customer_services_customer_status",
                table: "customer_services",
                columns: new[] { "customer_id", "status" });

            migrationBuilder.CreateIndex(
                name: "IX_customer_services_is_deleted",
                table: "customer_services",
                column: "is_deleted");

            migrationBuilder.CreateIndex(
                name: "ix_customer_services_status_next_invoice",
                table: "customer_services",
                columns: new[] { "status", "next_invoice_at" });

            migrationBuilder.CreateIndex(
                name: "ux_customer_services_order_id",
                table: "customer_services",
                column: "order_id",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "customer_services");

            migrationBuilder.DropIndex(
                name: "ix_invoices_order_type",
                table: "invoices");

            migrationBuilder.DeleteData(
                table: "notification_templates",
                keyColumn: "id",
                keyValue: new Guid("63333333-3333-3333-3333-333333333333"));

            migrationBuilder.DropColumn(
                name: "activated_at",
                table: "orders");

            migrationBuilder.DropColumn(
                name: "activated_by",
                table: "orders");

            migrationBuilder.CreateIndex(
                name: "ux_invoices_order_type",
                table: "invoices",
                columns: new[] { "order_id", "type" },
                unique: true);
        }
    }
}
