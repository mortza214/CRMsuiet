using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CrmSuite.BuildingBlocks.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Phase132FormSubmissionHistory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "form_submission_history",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    form_submission_id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    action = table.Column<string>(type: "varchar(30)", maxLength: 30, nullable: false, collation: "utf8mb4_persian_ci"),
                    modified_by = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    modified_at = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    previous_data_json = table.Column<string>(type: "longtext", nullable: true, collation: "utf8mb4_persian_ci"),
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
                    table.PrimaryKey("PK_form_submission_history", x => x.id);
                })
                .Annotation("Relational:Collation", "utf8mb4_persian_ci");

            migrationBuilder.CreateIndex(
                name: "IX_form_submission_history_is_deleted",
                table: "form_submission_history",
                column: "is_deleted");

            migrationBuilder.CreateIndex(
                name: "ix_form_submission_history_submission",
                table: "form_submission_history",
                column: "form_submission_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "form_submission_history");
        }
    }
}
