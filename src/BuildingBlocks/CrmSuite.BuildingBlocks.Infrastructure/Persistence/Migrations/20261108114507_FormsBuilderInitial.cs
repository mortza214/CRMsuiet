using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CrmSuite.BuildingBlocks.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class FormsBuilderInitial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "title",
                table: "form_templates",
                type: "varchar(200)",
                maxLength: 200,
                nullable: false,
                collation: "utf8mb4_persian_ci",
                oldClrType: typeof(string),
                oldType: "varchar(300)",
                oldMaxLength: 300)
                .OldAnnotation("Relational:Collation", "utf8mb4_persian_ci");

            migrationBuilder.AddColumn<DateTime>(
                name: "archived_at",
                table: "form_templates",
                type: "datetime(6)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "code",
                table: "form_templates",
                type: "varchar(64)",
                maxLength: 64,
                nullable: true,
                collation: "utf8mb4_persian_ci");

            migrationBuilder.AddColumn<string>(
                name: "entity_type",
                table: "form_templates",
                type: "varchar(30)",
                maxLength: 30,
                nullable: true,
                collation: "utf8mb4_persian_ci");

            migrationBuilder.AddColumn<DateTime>(
                name: "published_at",
                table: "form_templates",
                type: "datetime(6)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "status",
                table: "form_templates",
                type: "varchar(30)",
                maxLength: 30,
                nullable: true,
                collation: "utf8mb4_persian_ci");

            migrationBuilder.AddColumn<Guid>(
                name: "target_service_category_id",
                table: "form_templates",
                type: "char(36)",
                nullable: true,
                collation: "ascii_general_ci");

            migrationBuilder.AddColumn<Guid>(
                name: "target_service_id",
                table: "form_templates",
                type: "char(36)",
                nullable: true,
                collation: "ascii_general_ci");

            migrationBuilder.AlterColumn<Guid>(
                name: "customer_id",
                table: "form_submissions",
                type: "char(36)",
                nullable: true,
                collation: "ascii_general_ci",
                oldClrType: typeof(Guid),
                oldType: "char(36)")
                .OldAnnotation("Relational:Collation", "ascii_general_ci");

            migrationBuilder.AddColumn<Guid>(
                name: "entity_id",
                table: "form_submissions",
                type: "char(36)",
                nullable: true,
                collation: "ascii_general_ci");

            migrationBuilder.AddColumn<string>(
                name: "entity_type",
                table: "form_submissions",
                type: "varchar(30)",
                maxLength: 30,
                nullable: true,
                collation: "utf8mb4_persian_ci");

            migrationBuilder.AddColumn<int>(
                name: "form_version",
                table: "form_submissions",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "review_note",
                table: "form_submissions",
                type: "varchar(2000)",
                maxLength: 2000,
                nullable: true,
                collation: "utf8mb4_persian_ci");

            migrationBuilder.AddColumn<DateTime>(
                name: "reviewed_at",
                table: "form_submissions",
                type: "datetime(6)",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "reviewed_by_user_id",
                table: "form_submissions",
                type: "char(36)",
                nullable: true,
                collation: "ascii_general_ci");

            migrationBuilder.AddColumn<string>(
                name: "status",
                table: "form_submissions",
                type: "varchar(30)",
                maxLength: 30,
                nullable: true,
                collation: "utf8mb4_persian_ci");

            migrationBuilder.AddColumn<DateTime>(
                name: "submitted_at",
                table: "form_submissions",
                type: "datetime(6)",
                nullable: true);

            // Preserve and backfill existing rows before enforcing the new model.
            migrationBuilder.Sql("""
                UPDATE form_templates
                SET code = CONCAT('FORM-', REPLACE(id, '-', ''))
                WHERE code IS NULL OR code = '';

                UPDATE form_templates
                SET status = CASE WHEN is_active = 1 THEN 'Published' ELSE 'Draft' END
                WHERE status IS NULL OR status = '';

                UPDATE form_templates
                SET entity_type = 'None'
                WHERE entity_type IS NULL OR entity_type = '';

                UPDATE form_templates
                SET published_at = created_at
                WHERE status = 'Published' AND published_at IS NULL;

                UPDATE form_submissions s
                JOIN form_templates t ON t.id = s.form_template_id
                SET s.form_version = t.version
                WHERE s.form_version IS NULL OR s.form_version = 0;

                UPDATE form_submissions
                SET form_version = 1
                WHERE form_version IS NULL OR form_version = 0;

                UPDATE form_submissions
                SET entity_type = CASE WHEN customer_id IS NULL THEN 'None' ELSE 'Customer' END
                WHERE entity_type IS NULL OR entity_type = '';

                UPDATE form_submissions
                SET entity_id = customer_id
                WHERE entity_type = 'Customer' AND entity_id IS NULL;

                UPDATE form_submissions
                SET status = 'Submitted'
                WHERE status IS NULL OR status = '';

                UPDATE form_submissions
                SET submitted_at = created_at
                WHERE submitted_at IS NULL OR submitted_at = '0001-01-01 00:00:00';
                """);

            migrationBuilder.AlterColumn<string>(
                name: "code",
                table: "form_templates",
                type: "varchar(64)",
                maxLength: 64,
                nullable: false,
                collation: "utf8mb4_persian_ci",
                oldClrType: typeof(string),
                oldType: "varchar(64)",
                oldMaxLength: 64,
                oldNullable: true)
                .OldAnnotation("Relational:Collation", "utf8mb4_persian_ci");

            migrationBuilder.AlterColumn<string>(
                name: "status",
                table: "form_templates",
                type: "varchar(30)",
                maxLength: 30,
                nullable: false,
                collation: "utf8mb4_persian_ci",
                oldClrType: typeof(string),
                oldType: "varchar(30)",
                oldMaxLength: 30,
                oldNullable: true)
                .OldAnnotation("Relational:Collation", "utf8mb4_persian_ci");

            migrationBuilder.AlterColumn<string>(
                name: "entity_type",
                table: "form_templates",
                type: "varchar(30)",
                maxLength: 30,
                nullable: false,
                collation: "utf8mb4_persian_ci",
                oldClrType: typeof(string),
                oldType: "varchar(30)",
                oldMaxLength: 30,
                oldNullable: true)
                .OldAnnotation("Relational:Collation", "utf8mb4_persian_ci");

            migrationBuilder.AlterColumn<string>(
                name: "entity_type",
                table: "form_submissions",
                type: "varchar(30)",
                maxLength: 30,
                nullable: false,
                collation: "utf8mb4_persian_ci",
                oldClrType: typeof(string),
                oldType: "varchar(30)",
                oldMaxLength: 30,
                oldNullable: true)
                .OldAnnotation("Relational:Collation", "utf8mb4_persian_ci");

            migrationBuilder.AlterColumn<string>(
                name: "status",
                table: "form_submissions",
                type: "varchar(30)",
                maxLength: 30,
                nullable: false,
                collation: "utf8mb4_persian_ci",
                oldClrType: typeof(string),
                oldType: "varchar(30)",
                oldMaxLength: 30,
                oldNullable: true)
                .OldAnnotation("Relational:Collation", "utf8mb4_persian_ci");

            migrationBuilder.AlterColumn<int>(
                name: "form_version",
                table: "form_submissions",
                type: "int",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "submitted_at",
                table: "form_submissions",
                type: "datetime(6)",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "datetime(6)",
                oldNullable: true);
            migrationBuilder.CreateTable(
                name: "form_fields",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    form_definition_id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    field_key = table.Column<string>(type: "varchar(120)", maxLength: 120, nullable: false, collation: "utf8mb4_persian_ci"),
                    label = table.Column<string>(type: "varchar(300)", maxLength: 300, nullable: false, collation: "utf8mb4_persian_ci"),
                    field_type = table.Column<string>(type: "varchar(30)", maxLength: 30, nullable: false, collation: "utf8mb4_persian_ci"),
                    is_required = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    default_value = table.Column<string>(type: "varchar(2000)", maxLength: 2000, nullable: true, collation: "utf8mb4_persian_ci"),
                    options_json = table.Column<string>(type: "longtext", nullable: true, collation: "utf8mb4_persian_ci"),
                    min_value = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true, collation: "utf8mb4_persian_ci"),
                    max_value = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true, collation: "utf8mb4_persian_ci"),
                    regex_pattern = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true, collation: "utf8mb4_persian_ci"),
                    sort_order = table.Column<int>(type: "int", nullable: false),
                    visible_if_json = table.Column<string>(type: "longtext", nullable: true, collation: "utf8mb4_persian_ci"),
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
                    table.PrimaryKey("PK_form_fields", x => x.id);
                    table.ForeignKey(
                        name: "FK_form_fields_form_templates_form_definition_id",
                        column: x => x.form_definition_id,
                        principalTable: "form_templates",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("Relational:Collation", "utf8mb4_persian_ci");

            migrationBuilder.CreateTable(
                name: "form_submission_items",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    form_submission_id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    field_key = table.Column<string>(type: "varchar(120)", maxLength: 120, nullable: false, collation: "utf8mb4_persian_ci"),
                    field_type = table.Column<string>(type: "varchar(30)", maxLength: 30, nullable: false, collation: "utf8mb4_persian_ci"),
                    value_text = table.Column<string>(type: "longtext", nullable: true, collation: "utf8mb4_persian_ci"),
                    value_number = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: true),
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
                    table.PrimaryKey("PK_form_submission_items", x => x.id);
                    table.ForeignKey(
                        name: "FK_form_submission_items_form_submissions_form_submission_id",
                        column: x => x.form_submission_id,
                        principalTable: "form_submissions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("Relational:Collation", "utf8mb4_persian_ci");

            migrationBuilder.CreateIndex(
                name: "ix_form_templates_entity_status",
                table: "form_templates",
                columns: new[] { "entity_type", "status" });

            migrationBuilder.CreateIndex(
                name: "ix_form_templates_target_category",
                table: "form_templates",
                column: "target_service_category_id");

            migrationBuilder.CreateIndex(
                name: "ix_form_templates_target_service",
                table: "form_templates",
                column: "target_service_id");

            migrationBuilder.CreateIndex(
                name: "ux_form_templates_code",
                table: "form_templates",
                column: "code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_form_submissions_entity",
                table: "form_submissions",
                columns: new[] { "entity_type", "entity_id" });

            migrationBuilder.CreateIndex(
                name: "ix_form_submissions_submitted_at",
                table: "form_submissions",
                column: "submitted_at");

            migrationBuilder.CreateIndex(
                name: "ix_form_submissions_template_status",
                table: "form_submissions",
                columns: new[] { "form_template_id", "status" });

            migrationBuilder.CreateIndex(
                name: "ix_form_fields_definition_order",
                table: "form_fields",
                columns: new[] { "form_definition_id", "sort_order" });

            migrationBuilder.CreateIndex(
                name: "IX_form_fields_is_deleted",
                table: "form_fields",
                column: "is_deleted");

            migrationBuilder.CreateIndex(
                name: "ux_form_fields_definition_key",
                table: "form_fields",
                columns: new[] { "form_definition_id", "field_key" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_form_submission_items_is_deleted",
                table: "form_submission_items",
                column: "is_deleted");

            migrationBuilder.CreateIndex(
                name: "ix_form_submission_items_key_number",
                table: "form_submission_items",
                columns: new[] { "field_key", "value_number" });

            migrationBuilder.CreateIndex(
                name: "ix_form_submission_items_submission_key",
                table: "form_submission_items",
                columns: new[] { "form_submission_id", "field_key" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "form_fields");

            migrationBuilder.DropTable(
                name: "form_submission_items");

            migrationBuilder.DropIndex(
                name: "ix_form_templates_entity_status",
                table: "form_templates");

            migrationBuilder.DropIndex(
                name: "ix_form_templates_target_category",
                table: "form_templates");

            migrationBuilder.DropIndex(
                name: "ix_form_templates_target_service",
                table: "form_templates");

            migrationBuilder.DropIndex(
                name: "ux_form_templates_code",
                table: "form_templates");

            migrationBuilder.DropIndex(
                name: "ix_form_submissions_entity",
                table: "form_submissions");

            migrationBuilder.DropIndex(
                name: "ix_form_submissions_submitted_at",
                table: "form_submissions");

            migrationBuilder.DropIndex(
                name: "ix_form_submissions_template_status",
                table: "form_submissions");

            migrationBuilder.DropColumn(
                name: "archived_at",
                table: "form_templates");

            migrationBuilder.DropColumn(
                name: "code",
                table: "form_templates");

            migrationBuilder.DropColumn(
                name: "entity_type",
                table: "form_templates");

            migrationBuilder.DropColumn(
                name: "published_at",
                table: "form_templates");

            migrationBuilder.DropColumn(
                name: "status",
                table: "form_templates");

            migrationBuilder.DropColumn(
                name: "target_service_category_id",
                table: "form_templates");

            migrationBuilder.DropColumn(
                name: "target_service_id",
                table: "form_templates");

            migrationBuilder.DropColumn(
                name: "entity_id",
                table: "form_submissions");

            migrationBuilder.DropColumn(
                name: "entity_type",
                table: "form_submissions");

            migrationBuilder.DropColumn(
                name: "form_version",
                table: "form_submissions");

            migrationBuilder.DropColumn(
                name: "review_note",
                table: "form_submissions");

            migrationBuilder.DropColumn(
                name: "reviewed_at",
                table: "form_submissions");

            migrationBuilder.DropColumn(
                name: "reviewed_by_user_id",
                table: "form_submissions");

            migrationBuilder.DropColumn(
                name: "status",
                table: "form_submissions");

            migrationBuilder.DropColumn(
                name: "submitted_at",
                table: "form_submissions");

            migrationBuilder.AlterColumn<string>(
                name: "title",
                table: "form_templates",
                type: "varchar(300)",
                maxLength: 300,
                nullable: false,
                collation: "utf8mb4_persian_ci",
                oldClrType: typeof(string),
                oldType: "varchar(200)",
                oldMaxLength: 200)
                .OldAnnotation("Relational:Collation", "utf8mb4_persian_ci");

            migrationBuilder.AlterColumn<Guid>(
                name: "customer_id",
                table: "form_submissions",
                type: "char(36)",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                collation: "ascii_general_ci",
                oldClrType: typeof(Guid),
                oldType: "char(36)",
                oldNullable: true)
                .OldAnnotation("Relational:Collation", "ascii_general_ci");
        }
    }
}
