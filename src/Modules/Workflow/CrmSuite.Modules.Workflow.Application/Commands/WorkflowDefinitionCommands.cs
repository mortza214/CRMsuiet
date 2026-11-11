using CrmSuite.BuildingBlocks.Domain.Results;
using CrmSuite.Modules.Workflow.Application.Dtos;
using CrmSuite.Modules.Workflow.Domain.Enums;
using MediatR;

namespace CrmSuite.Modules.Workflow.Application.Commands;

public sealed record CreateWorkflowDefinitionCommand(
    string Code,
    string Name,
    WorkflowEntityType EntityType,
    string? Description) : IRequest<Result<WorkflowDefinitionDto>>;

public sealed record UpdateWorkflowDraftCommand(
    Guid DefinitionId,
    string Name,
    string? Description) : IRequest<Result<WorkflowDefinitionDto>>;

public sealed record PublishWorkflowDefinitionCommand(
    Guid DefinitionId) : IRequest<Result<WorkflowDefinitionDto>>;

public sealed record ArchiveWorkflowDefinitionCommand(
    Guid DefinitionId) : IRequest<Result<WorkflowDefinitionDto>>;

public sealed record AddWorkflowStateCommand(
    Guid DefinitionId,
    string Code,
    string Name,
    WorkflowStateType StateType,
    int Order,
    bool IsFinal,
    WorkflowAssigneeType AssigneeType,
    string? AssigneeValue,
    string? FormCode,
    string Color,
    WorkflowActionKey ActionKey = WorkflowActionKey.None,
    double X = 0,
    double Y = 0) : IRequest<Result<WorkflowStateDto>>;

public sealed record UpdateWorkflowStateCommand(
    Guid DefinitionId,
    Guid StateId,
    string Code,
    string Name,
    WorkflowStateType StateType,
    int Order,
    bool IsFinal,
    WorkflowAssigneeType AssigneeType,
    string? AssigneeValue,
    string? FormCode,
    string Color,
    WorkflowActionKey ActionKey = WorkflowActionKey.None,
    double X = 0,
    double Y = 0) : IRequest<Result<WorkflowStateDto>>;

public sealed record RemoveWorkflowStateCommand(
    Guid DefinitionId,
    Guid StateId) : IRequest<Result>;

public sealed record AddWorkflowTransitionCommand(
    Guid DefinitionId,
    Guid FromStateId,
    Guid ToStateId,
    string Name,
    WorkflowGuardType GuardType,
    string? RequiredPermission,
    string? GuardPluginKey,
    bool RequiresComment,
    int SortOrder) : IRequest<Result<WorkflowTransitionDto>>;

public sealed record RemoveWorkflowTransitionCommand(
    Guid DefinitionId,
    Guid TransitionId) : IRequest<Result>;

public sealed record BindServiceWorkflowCommand(
    Guid DefinitionId,
    Guid ServiceId) : IRequest<Result>;
