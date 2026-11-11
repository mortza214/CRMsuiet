using System.Diagnostics.CodeAnalysis;
using CrmSuite.Modules.Foundation.Application.Abstractions;
using CrmSuite.Modules.Workflow.Application.Dtos;
using CrmSuite.Modules.Workflow.Domain.Entities;

namespace CrmSuite.Modules.Workflow.Application.Mapping;

[SuppressMessage(
    "Performance",
    "CA1822:Mark members as static",
    Justification = "Projection methods intentionally share the mapper instance for DI consistency.")]
public sealed class WorkflowDtoMapper
{
    private readonly IJalaliDateService _jalali;

    public WorkflowDtoMapper(IJalaliDateService jalali)
    {
        _jalali = jalali;
    }

    public WorkflowDefinitionSummaryDto ToSummary(WorkflowDefinition definition)
    {
        var published = definition.PublishedAt.HasValue
            ? AsUtc(definition.PublishedAt.Value)
            : (DateTime?)null;

        return new WorkflowDefinitionSummaryDto(
            definition.Id,
            definition.Code,
            definition.Name,
            definition.EntityType.ToString(),
            definition.Version,
            definition.Status.ToString(),
            definition.Description,
            definition.States.Count,
            definition.Transitions.Count,
            published,
            published.HasValue ? _jalali.ToJalali(published.Value) : null,
            AsUtc(definition.CreatedAt),
            _jalali.ToJalali(AsUtc(definition.CreatedAt)));
    }

    public WorkflowDefinitionDto ToDefinition(WorkflowDefinition definition)
    {
        var published = definition.PublishedAt.HasValue
            ? AsUtc(definition.PublishedAt.Value)
            : (DateTime?)null;

        return new WorkflowDefinitionDto(
            definition.Id,
            definition.Code,
            definition.Name,
            definition.EntityType.ToString(),
            definition.Version,
            definition.Status.ToString(),
            definition.Description,
            published,
            published.HasValue ? _jalali.ToJalali(published.Value) : null,
            AsUtc(definition.CreatedAt),
            _jalali.ToJalali(AsUtc(definition.CreatedAt)),
            definition.States
                .OrderBy(state => state.Order)
                .Select(ToState)
                .ToArray(),
            definition.Transitions
                .OrderBy(transition => transition.SortOrder)
                .Select(ToTransition)
                .ToArray());
    }

    public WorkflowStateDto ToState(WorkflowState state) =>
        new(
            state.Id,
            state.Code,
            state.Name,
            state.StateType.ToString(),
            state.Order,
            state.IsFinal,
            state.AssigneeType.ToString(),
            state.AssigneeValue,
            state.FormCode,
            state.Color,
            state.ActionKey.ToString());

    public WorkflowTransitionDto ToTransition(WorkflowTransition transition) =>
        new(
            transition.Id,
            transition.FromStateId,
            transition.ToStateId,
            transition.Name,
            transition.GuardType.ToString(),
            transition.RequiredPermission,
            transition.GuardPluginKey,
            transition.RequiresComment,
            transition.SortOrder);

    public WorkflowInstanceDto ToInstance(
        WorkflowInstance instance,
        WorkflowDefinition definition)
    {
        var current = definition.FindState(instance.CurrentStateId);
        var available = definition.Transitions
            .Where(transition => transition.FromStateId == instance.CurrentStateId)
            .OrderBy(transition => transition.SortOrder)
            .Select(ToTransition)
            .ToArray();

        var tasks = instance.Tasks
            .OrderBy(task => task.CreatedAt)
            .Select(task => ToTask(task, instance, definition))
            .ToArray();

        var history = instance.History
            .OrderBy(item => item.ActionAt)
            .Select(item => ToHistory(item, definition))
            .ToArray();

        return new WorkflowInstanceDto(
            instance.Id,
            instance.DefinitionId,
            definition.Code,
            definition.Name,
            instance.DefinitionVersion,
            instance.EntityType.ToString(),
            instance.EntityId,
            instance.CurrentStateId,
            current.Code,
            current.Name,
            instance.Status.ToString(),
            instance.StartedBy,
            AsUtc(instance.StartedAt),
            _jalali.ToJalali(AsUtc(instance.StartedAt)),
            instance.CompletedAt.HasValue ? AsUtc(instance.CompletedAt.Value) : null,
            instance.CompletedAt.HasValue
                ? _jalali.ToJalali(AsUtc(instance.CompletedAt.Value))
                : null,
            instance.CorrelationId,
            tasks,
            history,
            available);
    }

    public WorkflowTaskDto ToTask(
        WorkflowTask task,
        WorkflowInstance instance,
        WorkflowDefinition definition)
    {
        var current = definition.FindState(instance.CurrentStateId);
        var state = definition.FindState(task.StateId);

        return new WorkflowTaskDto(
            task.Id,
            task.InstanceId,
            task.StateId,
            state.Name,
            task.Status.ToString(),
            task.AssignedUserId,
            task.AssignedRoleId,
            task.AssignedDepartmentId,
            task.DueDate.HasValue ? AsUtc(task.DueDate.Value) : null,
            task.DueDate.HasValue ? _jalali.ToJalali(AsUtc(task.DueDate.Value)) : null,
            AsUtc(task.CreatedAt),
            _jalali.ToJalali(AsUtc(task.CreatedAt)),
            definition.Code,
            definition.Name,
            instance.EntityType.ToString(),
            instance.EntityId,
            current.Name,
            definition.Transitions
                .Where(transition => transition.FromStateId == instance.CurrentStateId)
                .OrderBy(transition => transition.SortOrder)
                .Select(ToTransition)
                .ToArray(),
            EntityDisplayName: null,
            EntityReference: null,
            ActionKey: current.ActionKey.ToString(),
            FormCode: current.FormCode);
    }

    public WorkflowHistoryDto ToHistory(
        WorkflowHistory history,
        WorkflowDefinition definition)
    {
        var from = history.FromStateId.HasValue
            ? definition.States.FirstOrDefault(state => state.Id == history.FromStateId.Value)
            : null;
        var to = definition.States.FirstOrDefault(state => state.Id == history.ToStateId);
        var transition = history.TransitionId.HasValue
            ? definition.Transitions.FirstOrDefault(item => item.Id == history.TransitionId.Value)
            : null;

        return new WorkflowHistoryDto(
            history.Id,
            history.FromStateId,
            history.ToStateId,
            from?.Name,
            to?.Name ?? string.Empty,
            history.TransitionId,
            transition?.Name,
            history.ActionBy,
            AsUtc(history.ActionAt),
            _jalali.ToJalali(AsUtc(history.ActionAt)),
            history.Comment,
            history.PayloadJson);
    }

    private static DateTime AsUtc(DateTime value) =>
        value.Kind == DateTimeKind.Utc ? value : DateTime.SpecifyKind(value, DateTimeKind.Utc);
}
