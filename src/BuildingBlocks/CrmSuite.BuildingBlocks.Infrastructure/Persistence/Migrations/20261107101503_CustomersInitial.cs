using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace CrmSuite.BuildingBlocks.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class CustomersInitial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "customer_groups",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    name = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false, collation: "utf8mb4_persian_ci"),
                    description = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true, collation: "utf8mb4_persian_ci"),
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
                    table.PrimaryKey("PK_customer_groups", x => x.id);
                })
                .Annotation("Relational:Collation", "utf8mb4_persian_ci");

            migrationBuilder.CreateTable(
                name: "customer_otps",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    mobile = table.Column<string>(type: "varchar(11)", maxLength: 11, nullable: false, collation: "utf8mb4_persian_ci"),
                    code = table.Column<string>(type: "varchar(10)", maxLength: 10, nullable: false, collation: "utf8mb4_persian_ci"),
                    expires_at = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    is_used = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    attempt_count = table.Column<int>(type: "int", nullable: false),
                    requested_by_ip = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: true, collation: "utf8mb4_persian_ci"),
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
                    table.PrimaryKey("PK_customer_otps", x => x.id);
                })
                .Annotation("Relational:Collation", "utf8mb4_persian_ci");

            migrationBuilder.CreateTable(
                name: "customer_tags",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    name = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false, collation: "utf8mb4_persian_ci"),
                    color = table.Column<string>(type: "varchar(7)", maxLength: 7, nullable: false, collation: "utf8mb4_persian_ci"),
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
                    table.PrimaryKey("PK_customer_tags", x => x.id);
                })
                .Annotation("Relational:Collation", "utf8mb4_persian_ci");

            migrationBuilder.CreateTable(
                name: "customers",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    type = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false, collation: "utf8mb4_persian_ci"),
                    first_name = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true, collation: "utf8mb4_persian_ci"),
                    last_name = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true, collation: "utf8mb4_persian_ci"),
                    company_name = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: true, collation: "utf8mb4_persian_ci"),
                    economic_code = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: true, collation: "utf8mb4_persian_ci"),
                    national_id = table.Column<string>(type: "varchar(11)", maxLength: 11, nullable: false, collation: "utf8mb4_persian_ci"),
                    mobile = table.Column<string>(type: "varchar(11)", maxLength: 11, nullable: false, collation: "utf8mb4_persian_ci"),
                    phone = table.Column<string>(type: "varchar(30)", maxLength: 30, nullable: true, collation: "utf8mb4_persian_ci"),
                    email = table.Column<string>(type: "varchar(256)", maxLength: 256, nullable: true, collation: "utf8mb4_persian_ci"),
                    address = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true, collation: "utf8mb4_persian_ci"),
                    owner_user_id = table.Column<Guid>(type: "char(36)", nullable: true, collation: "ascii_general_ci"),
                    parent_customer_id = table.Column<Guid>(type: "char(36)", nullable: true, collation: "ascii_general_ci"),
                    is_reseller = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    group_id = table.Column<Guid>(type: "char(36)", nullable: true, collation: "ascii_general_ci"),
                    user_id = table.Column<Guid>(type: "char(36)", nullable: true, collation: "ascii_general_ci"),
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
                    table.PrimaryKey("PK_customers", x => x.id);
                    table.ForeignKey(
                        name: "FK_customers_customer_groups_group_id",
                        column: x => x.group_id,
                        principalTable: "customer_groups",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("Relational:Collation", "utf8mb4_persian_ci");

            migrationBuilder.CreateTable(
                name: "customer_tag_maps",
                columns: table => new
                {
                    customer_id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    tag_id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_customer_tag_maps", x => new { x.customer_id, x.tag_id });
                    table.ForeignKey(
                        name: "FK_customer_tag_maps_customer_tags_tag_id",
                        column: x => x.tag_id,
                        principalTable: "customer_tags",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_customer_tag_maps_customers_customer_id",
                        column: x => x.customer_id,
                        principalTable: "customers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("Relational:Collation", "utf8mb4_persian_ci");

            migrationBuilder.InsertData(
                table: "customer_groups",
                columns: new[] { "id", "created_at", "created_by_user_id", "deleted_at", "deleted_by_user_id", "description", "is_deleted", "name", "updated_at", "updated_by_user_id" },
                values: new object[,]
                {
                    { new Guid("a1111111-1111-1111-1111-111111111111"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, null, null, "گروه پیش‌فرض مشتریان", false, "عادی", null, null },
                    { new Guid("a2222222-2222-2222-2222-222222222222"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, null, null, "مشتریان ویژه", false, "ویژه", null, null },
                    { new Guid("a3333333-3333-3333-3333-333333333333"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, null, null, "گروه مشتریان ریسلر", false, "ریسلر", null, null }
                });

            migrationBuilder.InsertData(
                table: "customer_tags",
                columns: new[] { "id", "color", "created_at", "created_by_user_id", "deleted_at", "deleted_by_user_id", "is_deleted", "name", "updated_at", "updated_by_user_id" },
                values: new object[,]
                {
                    { new Guid("b1111111-1111-1111-1111-111111111111"), "#52c41a", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, null, null, false, "مشتری وفادار", null, null },
                    { new Guid("b2222222-2222-2222-2222-222222222222"), "#ff4d4f", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, null, null, false, "بدهکار", null, null }
                });

            migrationBuilder.CreateIndex(
                name: "IX_customer_groups_is_deleted",
                table: "customer_groups",
                column: "is_deleted");

            migrationBuilder.CreateIndex(
                name: "ux_customer_groups_name",
                table: "customer_groups",
                column: "name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_customer_otps_is_deleted",
                table: "customer_otps",
                column: "is_deleted");

            migrationBuilder.CreateIndex(
                name: "ix_customer_otps_mobile_used_expires",
                table: "customer_otps",
                columns: new[] { "mobile", "is_used", "expires_at" });

            migrationBuilder.CreateIndex(
                name: "ix_customer_tag_maps_tag_id",
                table: "customer_tag_maps",
                column: "tag_id");

            migrationBuilder.CreateIndex(
                name: "IX_customer_tags_is_deleted",
                table: "customer_tags",
                column: "is_deleted");

            migrationBuilder.CreateIndex(
                name: "ux_customer_tags_name",
                table: "customer_tags",
                column: "name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_customers_group_id",
                table: "customers",
                column: "group_id");

            migrationBuilder.CreateIndex(
                name: "IX_customers_is_deleted",
                table: "customers",
                column: "is_deleted");

            migrationBuilder.CreateIndex(
                name: "ix_customers_is_reseller",
                table: "customers",
                column: "is_reseller");

            migrationBuilder.CreateIndex(
                name: "ix_customers_owner_user_id",
                table: "customers",
                column: "owner_user_id");

            migrationBuilder.CreateIndex(
                name: "ix_customers_parent_customer_id",
                table: "customers",
                column: "parent_customer_id");

            migrationBuilder.CreateIndex(
                name: "ux_customers_mobile",
                table: "customers",
                column: "mobile",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ux_customers_national_id",
                table: "customers",
                column: "national_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ux_customers_user_id",
                table: "customers",
                column: "user_id",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "customer_otps");

            migrationBuilder.DropTable(
                name: "customer_tag_maps");

            migrationBuilder.DropTable(
                name: "customer_tags");

            migrationBuilder.DropTable(
                name: "customers");

            migrationBuilder.DropTable(
                name: "customer_groups");
        }
    }
}
