using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace CrmSuite.BuildingBlocks.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class FormsNotificationsInitial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "form_submissions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    form_template_id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    customer_id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    order_id = table.Column<Guid>(type: "char(36)", nullable: true, collation: "ascii_general_ci"),
                    submitted_data_json = table.Column<string>(type: "longtext", nullable: false, collation: "utf8mb4_persian_ci"),
                    submitted_by_user_id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
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
                    table.PrimaryKey("PK_form_submissions", x => x.id);
                })
                .Annotation("Relational:Collation", "utf8mb4_persian_ci");

            migrationBuilder.CreateTable(
                name: "form_templates",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    title = table.Column<string>(type: "varchar(300)", maxLength: 300, nullable: false, collation: "utf8mb4_persian_ci"),
                    schema_json = table.Column<string>(type: "longtext", nullable: false, collation: "utf8mb4_persian_ci"),
                    version = table.Column<int>(type: "int", nullable: false),
                    is_active = table.Column<bool>(type: "tinyint(1)", nullable: false),
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
                    table.PrimaryKey("PK_form_templates", x => x.id);
                })
                .Annotation("Relational:Collation", "utf8mb4_persian_ci");

            migrationBuilder.CreateTable(
                name: "notification_logs",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    recipient = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: false, collation: "utf8mb4_persian_ci"),
                    channel = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false, collation: "utf8mb4_persian_ci"),
                    template_code = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false, collation: "utf8mb4_persian_ci"),
                    content = table.Column<string>(type: "longtext", nullable: false, collation: "utf8mb4_persian_ci"),
                    status = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false, collation: "utf8mb4_persian_ci"),
                    error_message = table.Column<string>(type: "longtext", nullable: true, collation: "utf8mb4_persian_ci"),
                    is_read = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    sent_at = table.Column<DateTime>(type: "datetime(6)", nullable: true),
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
                    table.PrimaryKey("PK_notification_logs", x => x.id);
                })
                .Annotation("Relational:Collation", "utf8mb4_persian_ci");

            migrationBuilder.CreateTable(
                name: "notification_templates",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    code = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false, collation: "utf8mb4_persian_ci"),
                    title = table.Column<string>(type: "varchar(300)", maxLength: 300, nullable: false, collation: "utf8mb4_persian_ci"),
                    body = table.Column<string>(type: "longtext", nullable: false, collation: "utf8mb4_persian_ci"),
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
                    table.PrimaryKey("PK_notification_templates", x => x.id);
                })
                .Annotation("Relational:Collation", "utf8mb4_persian_ci");

            migrationBuilder.InsertData(
                table: "notification_templates",
                columns: new[] { "id", "body", "code", "created_at", "created_by_user_id", "deleted_at", "deleted_by_user_id", "is_deleted", "title", "updated_at", "updated_by_user_id" },
                values: new object[,]
                {
                    { new Guid("61111111-1111-1111-1111-111111111111"), "تیکت {TicketNumber} برای {CustomerName} با موضوع {Subject} ثبت شد.", "TicketCreated", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, null, null, false, "تیکت جدید", null, null },
                    { new Guid("62222222-2222-2222-2222-222222222222"), "فاکتور {InvoiceNumber} به مبلغ {Amount} تومان پرداخت شد. متشکریم، {CustomerName}.", "InvoicePaid", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, null, null, false, "پرداخت موفق", null, null }
                });

            migrationBuilder.CreateIndex(
                name: "IX_form_submissions_is_deleted",
                table: "form_submissions",
                column: "is_deleted");

            migrationBuilder.CreateIndex(
                name: "ix_form_submissions_template_customer",
                table: "form_submissions",
                columns: new[] { "form_template_id", "customer_id" });

            migrationBuilder.CreateIndex(
                name: "IX_form_templates_is_deleted",
                table: "form_templates",
                column: "is_deleted");

            migrationBuilder.CreateIndex(
                name: "ix_form_templates_title_version",
                table: "form_templates",
                columns: new[] { "title", "version" });

            migrationBuilder.CreateIndex(
                name: "IX_notification_logs_is_deleted",
                table: "notification_logs",
                column: "is_deleted");

            migrationBuilder.CreateIndex(
                name: "ix_notification_logs_recipient_channel_read",
                table: "notification_logs",
                columns: new[] { "recipient", "channel", "is_read" });

            migrationBuilder.CreateIndex(
                name: "IX_notification_templates_is_deleted",
                table: "notification_templates",
                column: "is_deleted");

            migrationBuilder.CreateIndex(
                name: "ux_notification_templates_code",
                table: "notification_templates",
                column: "code",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "form_submissions");

            migrationBuilder.DropTable(
                name: "form_templates");

            migrationBuilder.DropTable(
                name: "notification_logs");

            migrationBuilder.DropTable(
                name: "notification_templates");
        }
    }
}
