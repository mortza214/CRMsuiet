using CrmSuite.Modules.Workflow.Domain.Enums;

namespace CrmSuite.Modules.Workflow.Application.Dtos;

public sealed record WorkflowDefinitionSummaryDto(
    Guid Id,
    string Code,
    string Name,
    string EntityType,
    int Version,
    string Status,
    string? Description,
    int StateCount,
    int TransitionCount,
    DateTime? PublishedAt,
    string? PublishedAtJalali,
    DateTime CreatedAt,
    string CreatedAtJalali);

public sealed record WorkflowStateDto(
    Guid Id,
    string Code,
    string Name,
    string StateType,
    int Order,
    bool IsFinal,
    string AssigneeType,
    string? AssigneeValue,
    string? FormCode,
    string Color,
    string ActionKey = "None",
    double X = 0,
    double Y = 0);

public sealed record WorkflowTransitionDto(
    Guid Id,
    Guid FromStateId,
    Guid ToStateId,
    string Name,
    string GuardType,
    string? RequiredPermission,
    string? GuardPluginKey,
    bool RequiresComment,
    int SortOrder);

public sealed record WorkflowDefinitionDto(
    Guid Id,
    string Code,
    string Name,
    string EntityType,
    int Version,
    string Status,
    string? Description,
    DateTime? PublishedAt,
    string? PublishedAtJalali,
    DateTime CreatedAt,
    string CreatedAtJalali,
    IReadOnlyList<WorkflowStateDto> States,
    IReadOnlyList<WorkflowTransitionDto> Transitions);

public sealed record WorkflowTaskDto(
    Guid Id,
    Guid InstanceId,
    Guid StateId,
    string StateName,
    string Status,
    Guid? AssignedUserId,
    Guid? AssignedRoleId,
    Guid? AssignedDepartmentId,
    DateTime? DueDate,
    string? DueDateJalali,
    DateTime CreatedAt,
    string CreatedAtJalali,
    string DefinitionCode,
    string DefinitionName,
    string EntityType,
    Guid EntityId,
    string CurrentStateName,
    IReadOnlyList<WorkflowTransitionDto> AvailableTransitions,
    string? EntityDisplayName = null,
    string? EntityReference = null,
    string? ActionKey = null,
    string? FormCode = null);

public sealed record WorkflowHistoryDto(
    Guid Id,
    Guid? FromStateId,
    Guid ToStateId,
    string? FromStateName,
    string ToStateName,
    Guid? TransitionId,
    string? TransitionName,
    Guid? ActionBy,
    DateTime ActionAt,
    string ActionAtJalali,
    string? Comment,
    string? PayloadJson);

public sealed record WorkflowInstanceDto(
    Guid Id,
    Guid DefinitionId,
    string DefinitionCode,
    string DefinitionName,
    int DefinitionVersion,
    string EntityType,
    Guid EntityId,
    Guid CurrentStateId,
    string CurrentStateCode,
    string CurrentStateName,
    string Status,
    Guid? StartedBy,
    DateTime StartedAt,
    string StartedAtJalali,
    DateTime? CompletedAt,
    string? CompletedAtJalali,
    string? CorrelationId,
    IReadOnlyList<WorkflowTaskDto> Tasks,
    IReadOnlyList<WorkflowHistoryDto> History,
    IReadOnlyList<WorkflowTransitionDto> AvailableTransitions);
