using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CrmSuite.BuildingBlocks.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class WorkflowInitial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "workflow_instance_id",
                table: "tickets",
                type: "char(36)",
                nullable: true,
                collation: "ascii_general_ci");

            migrationBuilder.AddColumn<Guid>(
                name: "workflow_definition_id",
                table: "services",
                type: "char(36)",
                nullable: true,
                collation: "ascii_general_ci");

            migrationBuilder.AddColumn<Guid>(
                name: "workflow_instance_id",
                table: "orders",
                type: "char(36)",
                nullable: true,
                collation: "ascii_general_ci");

            migrationBuilder.CreateTable(
                name: "workflow_definitions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    code = table.Column<string>(type: "varchar(80)", maxLength: 80, nullable: false, collation: "utf8mb4_persian_ci"),
                    name = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: false, collation: "utf8mb4_persian_ci"),
                    entity_type = table.Column<string>(type: "varchar(30)", maxLength: 30, nullable: false, collation: "utf8mb4_persian_ci"),
                    version = table.Column<int>(type: "int", nullable: false),
                    status = table.Column<string>(type: "varchar(30)", maxLength: 30, nullable: false, collation: "utf8mb4_persian_ci"),
                    description = table.Column<string>(type: "varchar(1000)", maxLength: 1000, nullable: true, collation: "utf8mb4_persian_ci"),
                    published_at = table.Column<DateTime>(type: "datetime(6)", nullable: true),
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
                    table.PrimaryKey("PK_workflow_definitions", x => x.id);
                })
                .Annotation("Relational:Collation", "utf8mb4_persian_ci");

            migrationBuilder.CreateTable(
                name: "workflow_instances",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    definition_id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    definition_version = table.Column<int>(type: "int", nullable: false),
                    entity_type = table.Column<string>(type: "varchar(30)", maxLength: 30, nullable: false, collation: "utf8mb4_persian_ci"),
                    entity_id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    current_state_id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    status = table.Column<string>(type: "varchar(30)", maxLength: 30, nullable: false, collation: "utf8mb4_persian_ci"),
                    started_by = table.Column<Guid>(type: "char(36)", nullable: true, collation: "ascii_general_ci"),
                    started_at = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    completed_at = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    correlation_id = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true, collation: "utf8mb4_persian_ci"),
                    idempotency_key = table.Column<string>(type: "varchar(150)", maxLength: 150, nullable: true, collation: "utf8mb4_persian_ci"),
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
                    table.PrimaryKey("PK_workflow_instances", x => x.id);
                })
                .Annotation("Relational:Collation", "utf8mb4_persian_ci");

            migrationBuilder.CreateTable(
                name: "workflow_states",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    definition_id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    code = table.Column<string>(type: "varchar(80)", maxLength: 80, nullable: false, collation: "utf8mb4_persian_ci"),
                    name = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: false, collation: "utf8mb4_persian_ci"),
                    state_type = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false, collation: "utf8mb4_persian_ci"),
                    sort_order = table.Column<int>(type: "int", nullable: false),
                    is_final = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    assignee_type = table.Column<string>(type: "varchar(30)", maxLength: 30, nullable: false, collation: "utf8mb4_persian_ci"),
                    assignee_value = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true, collation: "utf8mb4_persian_ci"),
                    form_code = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true, collation: "utf8mb4_persian_ci"),
                    color = table.Column<string>(type: "varchar(30)", maxLength: 30, nullable: false, collation: "utf8mb4_persian_ci"),
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
                    table.PrimaryKey("PK_workflow_states", x => x.id);
                    table.ForeignKey(
                        name: "FK_workflow_states_workflow_definitions_definition_id",
                        column: x => x.definition_id,
                        principalTable: "workflow_definitions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("Relational:Collation", "utf8mb4_persian_ci");

            migrationBuilder.CreateTable(
                name: "workflow_history",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    instance_id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    from_state_id = table.Column<Guid>(type: "char(36)", nullable: true, collation: "ascii_general_ci"),
                    to_state_id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    transition_id = table.Column<Guid>(type: "char(36)", nullable: true, collation: "ascii_general_ci"),
                    action_by = table.Column<Guid>(type: "char(36)", nullable: true, collation: "ascii_general_ci"),
                    action_at = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    comment = table.Column<string>(type: "varchar(2000)", maxLength: 2000, nullable: true, collation: "utf8mb4_persian_ci"),
                    payload_json = table.Column<string>(type: "longtext", nullable: true, collation: "utf8mb4_persian_ci"),
                    idempotency_key = table.Column<string>(type: "varchar(150)", maxLength: 150, nullable: true, collation: "utf8mb4_persian_ci"),
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
                    table.PrimaryKey("PK_workflow_history", x => x.id);
                    table.ForeignKey(
                        name: "FK_workflow_history_workflow_instances_instance_id",
                        column: x => x.instance_id,
                        principalTable: "workflow_instances",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("Relational:Collation", "utf8mb4_persian_ci");

            migrationBuilder.CreateTable(
                name: "workflow_tasks",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    instance_id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    state_id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    assigned_user_id = table.Column<Guid>(type: "char(36)", nullable: true, collation: "ascii_general_ci"),
                    assigned_role_id = table.Column<Guid>(type: "char(36)", nullable: true, collation: "ascii_general_ci"),
                    assigned_department_id = table.Column<Guid>(type: "char(36)", nullable: true, collation: "ascii_general_ci"),
                    status = table.Column<string>(type: "varchar(30)", maxLength: 30, nullable: false, collation: "utf8mb4_persian_ci"),
                    due_date = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    completed_by = table.Column<Guid>(type: "char(36)", nullable: true, collation: "ascii_general_ci"),
                    completed_at = table.Column<DateTime>(type: "datetime(6)", nullable: true),
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
                    table.PrimaryKey("PK_workflow_tasks", x => x.id);
                    table.ForeignKey(
                        name: "FK_workflow_tasks_workflow_instances_instance_id",
                        column: x => x.instance_id,
                        principalTable: "workflow_instances",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("Relational:Collation", "utf8mb4_persian_ci");

            migrationBuilder.CreateTable(
                name: "workflow_transitions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    definition_id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    from_state_id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    to_state_id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    name = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: false, collation: "utf8mb4_persian_ci"),
                    guard_type = table.Column<string>(type: "varchar(30)", maxLength: 30, nullable: false, collation: "utf8mb4_persian_ci"),
                    required_permission = table.Column<string>(type: "varchar(150)", maxLength: 150, nullable: true, collation: "utf8mb4_persian_ci"),
                    guard_plugin_key = table.Column<string>(type: "varchar(150)", maxLength: 150, nullable: true, collation: "utf8mb4_persian_ci"),
                    requires_comment = table.Column<bool>(type: "tinyint(1)", nullable: false),
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
                    table.PrimaryKey("PK_workflow_transitions", x => x.id);
                    table.ForeignKey(
                        name: "FK_workflow_transitions_workflow_definitions_definition_id",
                        column: x => x.definition_id,
                        principalTable: "workflow_definitions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_workflow_transitions_workflow_states_from_state_id",
                        column: x => x.from_state_id,
                        principalTable: "workflow_states",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_workflow_transitions_workflow_states_to_state_id",
                        column: x => x.to_state_id,
                        principalTable: "workflow_states",
                        principalColumn: "id");
                })
                .Annotation("Relational:Collation", "utf8mb4_persian_ci");

            migrationBuilder.UpdateData(
                table: "services",
                keyColumn: "id",
                keyValue: new Guid("d1111111-1111-1111-1111-111111111111"),
                column: "workflow_definition_id",
                value: null);

            migrationBuilder.UpdateData(
                table: "services",
                keyColumn: "id",
                keyValue: new Guid("d2222222-2222-2222-2222-222222222222"),
                column: "workflow_definition_id",
                value: null);

            migrationBuilder.UpdateData(
                table: "services",
                keyColumn: "id",
                keyValue: new Guid("d3333333-3333-3333-3333-333333333333"),
                column: "workflow_definition_id",
                value: null);

            migrationBuilder.CreateIndex(
                name: "ix_tickets_workflow_instance_id",
                table: "tickets",
                column: "workflow_instance_id");

            migrationBuilder.CreateIndex(
                name: "ix_services_workflow_definition_id",
                table: "services",
                column: "workflow_definition_id");

            migrationBuilder.CreateIndex(
                name: "ix_orders_workflow_instance_id",
                table: "orders",
                column: "workflow_instance_id");

            migrationBuilder.CreateIndex(
                name: "ix_workflow_definitions_entity_status",
                table: "workflow_definitions",
                columns: new[] { "entity_type", "status" });

            migrationBuilder.CreateIndex(
                name: "IX_workflow_definitions_is_deleted",
                table: "workflow_definitions",
                column: "is_deleted");

            migrationBuilder.CreateIndex(
                name: "ux_workflow_definitions_code",
                table: "workflow_definitions",
                column: "code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_workflow_history_instance_action",
                table: "workflow_history",
                columns: new[] { "instance_id", "action_at" });

            migrationBuilder.CreateIndex(
                name: "IX_workflow_history_is_deleted",
                table: "workflow_history",
                column: "is_deleted");

            migrationBuilder.CreateIndex(
                name: "ux_workflow_history_instance_idempotency",
                table: "workflow_history",
                columns: new[] { "instance_id", "idempotency_key" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_workflow_instances_definition_status",
                table: "workflow_instances",
                columns: new[] { "definition_id", "status" });

            migrationBuilder.CreateIndex(
                name: "IX_workflow_instances_is_deleted",
                table: "workflow_instances",
                column: "is_deleted");

            migrationBuilder.CreateIndex(
                name: "ux_workflow_instances_entity_status",
                table: "workflow_instances",
                columns: new[] { "entity_type", "entity_id", "status" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ux_workflow_instances_idempotency_key",
                table: "workflow_instances",
                column: "idempotency_key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_workflow_states_definition_order",
                table: "workflow_states",
                columns: new[] { "definition_id", "sort_order" });

            migrationBuilder.CreateIndex(
                name: "IX_workflow_states_is_deleted",
                table: "workflow_states",
                column: "is_deleted");

            migrationBuilder.CreateIndex(
                name: "ux_workflow_states_definition_code",
                table: "workflow_states",
                columns: new[] { "definition_id", "code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_workflow_tasks_department_status",
                table: "workflow_tasks",
                columns: new[] { "assigned_department_id", "status" });

            migrationBuilder.CreateIndex(
                name: "ix_workflow_tasks_instance_state",
                table: "workflow_tasks",
                columns: new[] { "instance_id", "state_id" });

            migrationBuilder.CreateIndex(
                name: "IX_workflow_tasks_is_deleted",
                table: "workflow_tasks",
                column: "is_deleted");

            migrationBuilder.CreateIndex(
                name: "ix_workflow_tasks_role_status",
                table: "workflow_tasks",
                columns: new[] { "assigned_role_id", "status" });

            migrationBuilder.CreateIndex(
                name: "ix_workflow_tasks_user_status",
                table: "workflow_tasks",
                columns: new[] { "assigned_user_id", "status" });

            migrationBuilder.CreateIndex(
                name: "IX_workflow_transitions_from_state_id",
                table: "workflow_transitions",
                column: "from_state_id");

            migrationBuilder.CreateIndex(
                name: "IX_workflow_transitions_is_deleted",
                table: "workflow_transitions",
                column: "is_deleted");

            migrationBuilder.CreateIndex(
                name: "IX_workflow_transitions_to_state_id",
                table: "workflow_transitions",
                column: "to_state_id");

            migrationBuilder.CreateIndex(
                name: "ux_workflow_transitions_definition_from_to",
                table: "workflow_transitions",
                columns: new[] { "definition_id", "from_state_id", "to_state_id" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "workflow_history");

            migrationBuilder.DropTable(
                name: "workflow_tasks");

            migrationBuilder.DropTable(
                name: "workflow_transitions");

            migrationBuilder.DropTable(
                name: "workflow_instances");

            migrationBuilder.DropTable(
                name: "workflow_states");

            migrationBuilder.DropTable(
                name: "workflow_definitions");

            migrationBuilder.DropIndex(
                name: "ix_tickets_workflow_instance_id",
                table: "tickets");

            migrationBuilder.DropIndex(
                name: "ix_services_workflow_definition_id",
                table: "services");

            migrationBuilder.DropIndex(
                name: "ix_orders_workflow_instance_id",
                table: "orders");

            migrationBuilder.DropColumn(
                name: "workflow_instance_id",
                table: "tickets");

            migrationBuilder.DropColumn(
                name: "workflow_definition_id",
                table: "services");

            migrationBuilder.DropColumn(
                name: "workflow_instance_id",
                table: "orders");
        }
    }
}
