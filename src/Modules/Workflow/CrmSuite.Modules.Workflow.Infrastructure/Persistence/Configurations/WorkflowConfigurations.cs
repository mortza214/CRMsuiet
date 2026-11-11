using CrmSuite.Modules.Foundation.Infrastructure.Persistence.Configurations;
using CrmSuite.Modules.Workflow.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CrmSuite.Modules.Workflow.Infrastructure.Persistence.Configurations;

public sealed class WorkflowDefinitionConfiguration : BaseEntityConfiguration<WorkflowDefinition>
{
    protected override void ConfigureEntity(EntityTypeBuilder<WorkflowDefinition> builder)
    {
        builder.ToTable("workflow_definitions");
        builder.Property(definition => definition.Code).HasColumnName("code").HasMaxLength(80).IsRequired();
        builder.HasIndex(definition => definition.Code).IsUnique().HasDatabaseName("ux_workflow_definitions_code");
        builder.Property(definition => definition.Name).HasColumnName("name").HasMaxLength(200).IsRequired();
        builder.Property(definition => definition.EntityType).HasColumnName("entity_type").HasConversion<string>().HasMaxLength(30).IsRequired();
        builder.Property(definition => definition.Version).HasColumnName("version").IsRequired();
        builder.Property(definition => definition.Status).HasColumnName("status").HasConversion<string>().HasMaxLength(30).IsRequired();
        builder.Property(definition => definition.Description).HasColumnName("description").HasMaxLength(1000);
        builder.Property(definition => definition.PublishedAt).HasColumnName("published_at").HasColumnType("datetime(6)");
        builder.HasIndex(definition => new { definition.EntityType, definition.Status }).HasDatabaseName("ix_workflow_definitions_entity_status");

        builder.HasMany(definition => definition.States)
            .WithOne()
            .HasForeignKey(state => state.DefinitionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(definition => definition.Transitions)
            .WithOne()
            .HasForeignKey(transition => transition.DefinitionId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public sealed class WorkflowStateConfiguration : BaseEntityConfiguration<WorkflowState>
{
    protected override void ConfigureEntity(EntityTypeBuilder<WorkflowState> builder)
    {
        builder.ToTable("workflow_states");
        builder.Property(state => state.DefinitionId).HasColumnName("definition_id").IsRequired();
        builder.Property(state => state.Code).HasColumnName("code").HasMaxLength(80).IsRequired();
        builder.Property(state => state.Name).HasColumnName("name").HasMaxLength(200).IsRequired();
        builder.Property(state => state.StateType).HasColumnName("state_type").HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(state => state.Order).HasColumnName("sort_order").IsRequired();
        builder.Property(state => state.IsFinal).HasColumnName("is_final").IsRequired();
        builder.Property(state => state.AssigneeType).HasColumnName("assignee_type").HasConversion<string>().HasMaxLength(30).IsRequired();
        builder.Property(state => state.AssigneeValue).HasColumnName("assignee_value").HasMaxLength(100);
        builder.Property(state => state.FormCode).HasColumnName("form_code").HasMaxLength(100);
        builder.Property(state => state.ActionKey)
            .HasColumnName("action_key")
            .HasConversion<string>()
            .HasMaxLength(30)
            .IsRequired();
        builder.Property(state => state.X).HasColumnName("x").HasColumnType("double").IsRequired();
        builder.Property(state => state.Y).HasColumnName("y").HasColumnType("double").IsRequired();
        builder.Property(state => state.Color).HasColumnName("color").HasMaxLength(30).IsRequired();
        builder.HasIndex(state => new { state.DefinitionId, state.Code }).IsUnique().HasDatabaseName("ux_workflow_states_definition_code");
        builder.HasIndex(state => new { state.DefinitionId, state.Order }).HasDatabaseName("ix_workflow_states_definition_order");
    }
}

public sealed class WorkflowTransitionConfiguration : BaseEntityConfiguration<WorkflowTransition>
{
    protected override void ConfigureEntity(EntityTypeBuilder<WorkflowTransition> builder)
    {
        builder.ToTable("workflow_transitions");
        builder.Property(transition => transition.DefinitionId).HasColumnName("definition_id").IsRequired();
        builder.Property(transition => transition.FromStateId).HasColumnName("from_state_id").IsRequired();
        builder.Property(transition => transition.ToStateId).HasColumnName("to_state_id").IsRequired();
        builder.Property(transition => transition.Name).HasColumnName("name").HasMaxLength(200).IsRequired();
        builder.Property(transition => transition.GuardType).HasColumnName("guard_type").HasConversion<string>().HasMaxLength(30).IsRequired();
        builder.Property(transition => transition.RequiredPermission).HasColumnName("required_permission").HasMaxLength(150);
        builder.Property(transition => transition.GuardPluginKey).HasColumnName("guard_plugin_key").HasMaxLength(150);
        builder.Property(transition => transition.RequiresComment).HasColumnName("requires_comment").IsRequired();
        builder.Property(transition => transition.SortOrder).HasColumnName("sort_order").IsRequired();
        builder.HasIndex(transition => new { transition.DefinitionId, transition.FromStateId, transition.ToStateId })
            .IsUnique()
            .HasDatabaseName("ux_workflow_transitions_definition_from_to");

        builder.HasOne<WorkflowState>()
            .WithMany()
            .HasForeignKey(transition => transition.FromStateId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasOne<WorkflowState>()
            .WithMany()
            .HasForeignKey(transition => transition.ToStateId)
            .OnDelete(DeleteBehavior.NoAction);
    }
}

public sealed class WorkflowInstanceConfiguration : BaseEntityConfiguration<WorkflowInstance>
{
    protected override void ConfigureEntity(EntityTypeBuilder<WorkflowInstance> builder)
    {
        builder.ToTable("workflow_instances");
        builder.Property(instance => instance.DefinitionId).HasColumnName("definition_id").IsRequired();
        builder.Property(instance => instance.DefinitionVersion).HasColumnName("definition_version").IsRequired();
        builder.Property(instance => instance.EntityType).HasColumnName("entity_type").HasConversion<string>().HasMaxLength(30).IsRequired();
        builder.Property(instance => instance.EntityId).HasColumnName("entity_id").IsRequired();
        builder.Property(instance => instance.CurrentStateId).HasColumnName("current_state_id").IsRequired();
        builder.Property(instance => instance.Status).HasColumnName("status").HasConversion<string>().HasMaxLength(30).IsRequired();
        builder.Property(instance => instance.StartedBy).HasColumnName("started_by");
        builder.Property(instance => instance.StartedAt).HasColumnName("started_at").HasColumnType("datetime(6)").IsRequired();
        builder.Property(instance => instance.CompletedAt).HasColumnName("completed_at").HasColumnType("datetime(6)");
        builder.Property(instance => instance.CorrelationId).HasColumnName("correlation_id").HasMaxLength(100);
        builder.Property(instance => instance.IdempotencyKey).HasColumnName("idempotency_key").HasMaxLength(150);
        builder.HasIndex(instance => new { instance.EntityType, instance.EntityId, instance.Status })
            .IsUnique()
            .HasDatabaseName("ux_workflow_instances_entity_status");
        builder.HasIndex(instance => instance.IdempotencyKey).IsUnique().HasDatabaseName("ux_workflow_instances_idempotency_key");
        builder.HasIndex(instance => new { instance.DefinitionId, instance.Status }).HasDatabaseName("ix_workflow_instances_definition_status");

        builder.HasMany(instance => instance.Tasks)
            .WithOne()
            .HasForeignKey(task => task.InstanceId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(instance => instance.History)
            .WithOne()
            .HasForeignKey(history => history.InstanceId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public sealed class WorkflowTaskConfiguration : BaseEntityConfiguration<WorkflowTask>
{
    protected override void ConfigureEntity(EntityTypeBuilder<WorkflowTask> builder)
    {
        builder.ToTable("workflow_tasks");
        builder.Property(task => task.InstanceId).HasColumnName("instance_id").IsRequired();
        builder.Property(task => task.StateId).HasColumnName("state_id").IsRequired();
        builder.Property(task => task.AssignedUserId).HasColumnName("assigned_user_id");
        builder.Property(task => task.AssignedRoleId).HasColumnName("assigned_role_id");
        builder.Property(task => task.ActionKey).HasColumnName("action_key").HasMaxLength(40);
        builder.Property(task => task.FormCode).HasColumnName("form_code").HasMaxLength(100);
        builder.Property(task => task.AssignedDepartmentId).HasColumnName("assigned_department_id");
        builder.Property(task => task.Status).HasColumnName("status").HasConversion<string>().HasMaxLength(30).IsRequired();
        builder.Property(task => task.DueDate).HasColumnName("due_date").HasColumnType("datetime(6)");
        builder.Property(task => task.CompletedBy).HasColumnName("completed_by");
        builder.Property(task => task.CompletedAt).HasColumnName("completed_at").HasColumnType("datetime(6)");
        builder.HasIndex(task => new { task.AssignedUserId, task.Status }).HasDatabaseName("ix_workflow_tasks_user_status");
        builder.HasIndex(task => new { task.AssignedRoleId, task.Status }).HasDatabaseName("ix_workflow_tasks_role_status");
        builder.HasIndex(task => new { task.AssignedDepartmentId, task.Status }).HasDatabaseName("ix_workflow_tasks_department_status");
        builder.HasIndex(task => new { task.InstanceId, task.StateId }).HasDatabaseName("ix_workflow_tasks_instance_state");
    }
}

public sealed class WorkflowHistoryConfiguration : BaseEntityConfiguration<WorkflowHistory>
{
    protected override void ConfigureEntity(EntityTypeBuilder<WorkflowHistory> builder)
    {
        builder.ToTable("workflow_history");
        builder.Property(history => history.InstanceId).HasColumnName("instance_id").IsRequired();
        builder.Property(history => history.FromStateId).HasColumnName("from_state_id");
        builder.Property(history => history.ToStateId).HasColumnName("to_state_id").IsRequired();
        builder.Property(history => history.TransitionId).HasColumnName("transition_id");
        builder.Property(history => history.ActionBy).HasColumnName("action_by");
        builder.Property(history => history.ActionAt).HasColumnName("action_at").HasColumnType("datetime(6)").IsRequired();
        builder.Property(history => history.Comment).HasColumnName("comment").HasMaxLength(2000);
        builder.Property(history => history.PayloadJson).HasColumnName("payload_json").HasColumnType("longtext");
        builder.Property(history => history.IdempotencyKey).HasColumnName("idempotency_key").HasMaxLength(150);
        builder.HasIndex(history => new { history.InstanceId, history.ActionAt }).HasDatabaseName("ix_workflow_history_instance_action");
        builder.HasIndex(history => new { history.InstanceId, history.IdempotencyKey })
            .IsUnique()
            .HasDatabaseName("ux_workflow_history_instance_idempotency");
    }
}
