using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CrmSuite.BuildingBlocks.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class CatalogPricingAdvanced : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "attribute_schema_json",
                table: "services",
                type: "longtext",
                nullable: true,
                collation: "utf8mb4_persian_ci");

            migrationBuilder.AddColumn<string>(
                name: "billing_period",
                table: "services",
                type: "varchar(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "",
                collation: "utf8mb4_persian_ci");

            migrationBuilder.AddColumn<decimal>(
                name: "commission_percent",
                table: "services",
                type: "decimal(5,2)",
                precision: 5,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "pricing_mode",
                table: "services",
                type: "varchar(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "",
                collation: "utf8mb4_persian_ci");

            migrationBuilder.AddColumn<string>(
                name: "currency",
                table: "service_prices",
                type: "varchar(10)",
                maxLength: 10,
                nullable: false,
                defaultValue: "",
                collation: "utf8mb4_persian_ci");

            migrationBuilder.AddColumn<string>(
                name: "code",
                table: "service_addons",
                type: "varchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "",
                collation: "utf8mb4_persian_ci");

            migrationBuilder.AddColumn<string>(
                name: "description",
                table: "service_addons",
                type: "varchar(1000)",
                maxLength: 1000,
                nullable: true,
                collation: "utf8mb4_persian_ci");

            migrationBuilder.AddColumn<bool>(
                name: "is_active",
                table: "service_addons",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "pricing_mode",
                table: "service_addons",
                type: "varchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "",
                collation: "utf8mb4_persian_ci");

            migrationBuilder.AddColumn<Guid>(
                name: "service_id",
                table: "service_addons",
                type: "char(36)",
                nullable: true,
                collation: "ascii_general_ci");

            migrationBuilder.AddColumn<int>(
                name: "sort_order",
                table: "service_addons",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "addons_json",
                table: "order_items",
                type: "longtext",
                nullable: true,
                collation: "utf8mb4_persian_ci");

            migrationBuilder.AddColumn<decimal>(
                name: "addons_total",
                table: "order_items",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "attributes_json",
                table: "order_items",
                type: "longtext",
                nullable: true,
                collation: "utf8mb4_persian_ci");

            migrationBuilder.AddColumn<Guid>(
                name: "override_by",
                table: "order_items",
                type: "char(36)",
                nullable: true,
                collation: "ascii_general_ci");

            migrationBuilder.AddColumn<decimal>(
                name: "override_price",
                table: "order_items",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "override_reason",
                table: "order_items",
                type: "varchar(500)",
                maxLength: 500,
                nullable: true,
                collation: "utf8mb4_persian_ci");

            migrationBuilder.AddColumn<decimal>(
                name: "quantity_decimal",
                table: "order_items",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "unit_price_snapshot",
                table: "order_items",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            // Backfill existing catalog rows before new constraints/read paths are used.
            migrationBuilder.Sql("""
                UPDATE services
                SET pricing_mode = 'UnitBased'
                WHERE pricing_mode IS NULL OR pricing_mode = '';

                UPDATE services
                SET billing_period = CASE
                    WHEN service_type = 'PhysicalProduct' THEN 'OneTime'
                    ELSE 'Monthly'
                END
                WHERE billing_period IS NULL OR billing_period = '';

                UPDATE service_prices
                SET currency = 'IRR'
                WHERE currency IS NULL OR currency = '';

                UPDATE service_addons
                SET code = CONCAT('ADD-', REPLACE(id, '-', ''))
                WHERE code IS NULL OR code = '';

                UPDATE service_addons
                SET pricing_mode = 'Fixed'
                WHERE pricing_mode IS NULL OR pricing_mode = '';

                UPDATE service_addons
                SET is_active = 1
                WHERE is_active = 0;

                UPDATE order_items
                SET unit_price_snapshot = unit_price
                WHERE unit_price_snapshot = 0;

                UPDATE order_items
                SET quantity_decimal = quantity
                WHERE quantity_decimal = 0;

                UPDATE order_items
                SET addons_total = 0
                WHERE addons_total IS NULL;
                """);
            migrationBuilder.CreateTable(
                name: "service_form_links",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    service_id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    form_definition_id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    is_required = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    sort_order = table.Column<int>(type: "int", nullable: false),
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
                    table.PrimaryKey("PK_service_form_links", x => x.id);
                    table.ForeignKey(
                        name: "FK_service_form_links_services_service_id",
                        column: x => x.service_id,
                        principalTable: "services",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("Relational:Collation", "utf8mb4_persian_ci");

            migrationBuilder.UpdateData(
                table: "service_addons",
                keyColumn: "id",
                keyValue: new Guid("f1111111-1111-1111-1111-111111111111"),
                columns: new[] { "code", "description", "is_active", "pricing_mode", "service_id", "sort_order" },
                values: new object[] { "SEED-ADDON-2", null, true, "Fixed", null, 0 });

            migrationBuilder.UpdateData(
                table: "service_addons",
                keyColumn: "id",
                keyValue: new Guid("f2222222-2222-2222-2222-222222222222"),
                columns: new[] { "code", "description", "is_active", "pricing_mode", "service_id", "sort_order" },
                values: new object[] { "SEED-ADDON-1", null, true, "Fixed", null, 0 });

            migrationBuilder.UpdateData(
                table: "service_prices",
                keyColumn: "id",
                keyValue: new Guid("e1111111-1111-1111-1111-111111111111"),
                column: "currency",
                value: "IRR");

            migrationBuilder.UpdateData(
                table: "service_prices",
                keyColumn: "id",
                keyValue: new Guid("e2222222-2222-2222-2222-222222222222"),
                column: "currency",
                value: "IRR");

            migrationBuilder.UpdateData(
                table: "service_prices",
                keyColumn: "id",
                keyValue: new Guid("e3333333-3333-3333-3333-333333333333"),
                column: "currency",
                value: "IRR");

            migrationBuilder.UpdateData(
                table: "services",
                keyColumn: "id",
                keyValue: new Guid("d1111111-1111-1111-1111-111111111111"),
                columns: new[] { "attribute_schema_json", "billing_period", "commission_percent", "pricing_mode" },
                values: new object[] { null, "Monthly", null, "UnitBased" });

            migrationBuilder.UpdateData(
                table: "services",
                keyColumn: "id",
                keyValue: new Guid("d2222222-2222-2222-2222-222222222222"),
                columns: new[] { "attribute_schema_json", "billing_period", "commission_percent", "pricing_mode" },
                values: new object[] { null, "Monthly", null, "UnitBased" });

            migrationBuilder.UpdateData(
                table: "services",
                keyColumn: "id",
                keyValue: new Guid("d3333333-3333-3333-3333-333333333333"),
                columns: new[] { "attribute_schema_json", "billing_period", "commission_percent", "pricing_mode" },
                values: new object[] { null, "Monthly", null, "UnitBased" });

            migrationBuilder.CreateIndex(
                name: "ix_service_addons_code",
                table: "service_addons",
                column: "code");

            migrationBuilder.CreateIndex(
                name: "ix_service_addons_service_active",
                table: "service_addons",
                columns: new[] { "service_id", "is_active" });

            migrationBuilder.CreateIndex(
                name: "IX_service_form_links_is_deleted",
                table: "service_form_links",
                column: "is_deleted");

            migrationBuilder.CreateIndex(
                name: "ix_service_form_links_service",
                table: "service_form_links",
                column: "service_id");

            migrationBuilder.CreateIndex(
                name: "ux_service_form_links_service_form",
                table: "service_form_links",
                columns: new[] { "service_id", "form_definition_id" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "service_form_links");

            migrationBuilder.DropIndex(
                name: "ix_service_addons_code",
                table: "service_addons");

            migrationBuilder.DropIndex(
                name: "ix_service_addons_service_active",
                table: "service_addons");

            migrationBuilder.DropColumn(
                name: "attribute_schema_json",
                table: "services");

            migrationBuilder.DropColumn(
                name: "billing_period",
                table: "services");

            migrationBuilder.DropColumn(
                name: "commission_percent",
                table: "services");

            migrationBuilder.DropColumn(
                name: "pricing_mode",
                table: "services");

            migrationBuilder.DropColumn(
                name: "currency",
                table: "service_prices");

            migrationBuilder.DropColumn(
                name: "code",
                table: "service_addons");

            migrationBuilder.DropColumn(
                name: "description",
                table: "service_addons");

            migrationBuilder.DropColumn(
                name: "is_active",
                table: "service_addons");

            migrationBuilder.DropColumn(
                name: "pricing_mode",
                table: "service_addons");

            migrationBuilder.DropColumn(
                name: "service_id",
                table: "service_addons");

            migrationBuilder.DropColumn(
                name: "sort_order",
                table: "service_addons");

            migrationBuilder.DropColumn(
                name: "addons_json",
                table: "order_items");

            migrationBuilder.DropColumn(
                name: "addons_total",
                table: "order_items");

            migrationBuilder.DropColumn(
                name: "attributes_json",
                table: "order_items");

            migrationBuilder.DropColumn(
                name: "override_by",
                table: "order_items");

            migrationBuilder.DropColumn(
                name: "override_price",
                table: "order_items");

            migrationBuilder.DropColumn(
                name: "override_reason",
                table: "order_items");

            migrationBuilder.DropColumn(
                name: "quantity_decimal",
                table: "order_items");

            migrationBuilder.DropColumn(
                name: "unit_price_snapshot",
                table: "order_items");
        }
    }
}
