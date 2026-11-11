using CrmSuite.Modules.Workflow.Domain.Enums;

namespace CrmSuite.Modules.Workflow.Api.Contracts;

public sealed class CreateWorkflowDefinitionRequest
{
    public string Code { get; init; } = string.Empty;

    public string Name { get; init; } = string.Empty;

    public WorkflowEntityType EntityType { get; init; }

    public string? Description { get; init; }
}

public sealed class UpdateWorkflowDefinitionRequest
{
    public string Name { get; init; } = string.Empty;

    public string? Description { get; init; }
}

public sealed class WorkflowStateRequest
{
    public string Code { get; init; } = string.Empty;

    public string Name { get; init; } = string.Empty;

    public WorkflowStateType StateType { get; init; }

    public int Order { get; init; }

    public bool IsFinal { get; init; }

    public WorkflowAssigneeType AssigneeType { get; init; }

    public string? AssigneeValue { get; init; }

    public string? FormCode { get; init; }

    public WorkflowActionKey ActionKey { get; init; } = WorkflowActionKey.None;

    public string Color { get; init; } = "#1677ff";
}

public sealed class WorkflowTransitionRequest
{
    public Guid FromStateId { get; init; }

    public Guid ToStateId { get; init; }

    public string Name { get; init; } = string.Empty;

    public WorkflowGuardType GuardType { get; init; }

    public string? RequiredPermission { get; init; }

    public string? GuardPluginKey { get; init; }

    public bool RequiresComment { get; init; }

    public int SortOrder { get; init; }
}

public sealed class StartWorkflowRequest
{
    public Guid? DefinitionId { get; init; }

    public WorkflowEntityType EntityType { get; init; }

    public Guid EntityId { get; init; }

    public string? DefinitionCode { get; init; }

    public string? InitialPayload { get; init; }

    public string? IdempotencyKey { get; init; }
}

public sealed class ExecuteWorkflowTransitionRequest
{
    public Guid TransitionId { get; init; }

    public string? Comment { get; init; }

    public string? PayloadJson { get; init; }

    public string? IdempotencyKey { get; init; }
}

public sealed class ExecuteWorkflowActionRequest
{
    public Guid? TransitionId { get; init; }

    public string? ActionDataJson { get; init; }
}

public sealed class CloneWorkflowDefinitionRequest
{
    public string? Code { get; init; }
}

public sealed class CancelWorkflowRequest
{
    public string? Reason { get; init; }
}

public sealed class ReassignWorkflowTaskRequest
{
    public Guid? UserId { get; init; }

    public Guid? RoleId { get; init; }

    public Guid? DepartmentId { get; init; }
}
