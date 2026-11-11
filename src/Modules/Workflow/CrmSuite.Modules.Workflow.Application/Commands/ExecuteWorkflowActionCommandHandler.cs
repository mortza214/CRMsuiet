using System.Text.Json;
using CrmSuite.BuildingBlocks.Domain.Results;
using CrmSuite.Modules.Workflow.Application.Actions;
using CrmSuite.Modules.Workflow.Application.Dtos;
using CrmSuite.Modules.Workflow.Application.Queries;
using CrmSuite.Modules.Workflow.Application.Sales;
using CrmSuite.Modules.Workflow.Domain.Entities;
using CrmSuite.Modules.Workflow.Domain.Enums;
using MediatR;

namespace CrmSuite.Modules.Workflow.Application.Commands;

/// <summary>
/// Orchestrates a workflow action. Business logic is implemented by IWorkflowAction
/// implementations; this handler only resolves the action and executes the transition.
/// </summary>
public sealed class ExecuteWorkflowActionCommandHandler
    : IRequestHandler<ExecuteWorkflowActionCommand, Result<WorkflowInstanceDto>>
{
    private readonly BandwidthPhase6Support _support;
    private readonly IEnumerable<IWorkflowAction> _actions;
    private readonly IMediator _mediator;

    public ExecuteWorkflowActionCommandHandler(
        BandwidthPhase6Support support,
        IEnumerable<IWorkflowAction> actions,
        IMediator mediator)
    {
        _support = support;
        _actions = actions;
        _mediator = mediator;
    }

    public async Task<Result<WorkflowInstanceDto>> Handle(
        ExecuteWorkflowActionCommand request,
        CancellationToken cancellationToken)
    {
        var loaded = await _support.LoadAsync(request.InstanceId, cancellationToken);
        if (loaded is null)
        {
            return Result.Failure<WorkflowInstanceDto>(
                "گردش کار فعال یافت نشد.",
                "WORKFLOW_ACTION_NOT_ACTIVE");
        }

        var (order, instance, definition) = loaded.Value;
        var state = definition.FindState(instance.CurrentStateId);
        var transitionId = request.TransitionId
                           ?? definition.Transitions
                               .Where(item => item.FromStateId == state.Id)
                               .OrderBy(item => item.SortOrder)
                               .Select(item => (Guid?)item.Id)
                               .FirstOrDefault();
        if (!transitionId.HasValue
            && state.ActionKey != WorkflowActionKey.ActivateService)
        {
            return Result.Failure<WorkflowInstanceDto>(
                "گذاری برای این مرحله تعریف نشده است.",
                "WORKFLOW_TRANSITION_NOT_FOUND");
        }

        var selectedTransition = transitionId.HasValue
            ? definition.Transitions.FirstOrDefault(item => item.Id == transitionId.Value)
            : null;
        if (selectedTransition is not null
            && TryReadNeedsVisit(request.ActionDataJson, out var needsVisit))
        {
            var targetState = definition.FindState(selectedTransition.ToStateId);
            var targetsSiteVisit = targetState.Code == "SITE_VISIT_PENDING";
            if (needsVisit && !targetsSiteVisit)
            {
                return Result.Failure<WorkflowInstanceDto>(
                    "برای نیاز به مراجعه، باید گزینه ارجاع به کارشناس را انتخاب کنید.",
                    "WORKFLOW_FEASIBILITY_VISIT_REQUIRED");
            }

            if (!needsVisit && targetsSiteVisit)
            {
                return Result.Failure<WorkflowInstanceDto>(
                    "برای عدم نیاز به مراجعه، باید گزینه ادامه بدون مراجعه را انتخاب کنید.",
                    "WORKFLOW_FEASIBILITY_VISIT_NOT_REQUIRED");
            }
        }

        if (selectedTransition is not null
            && TryReadString(request.ActionDataJson, "equipment_source", out var equipmentSource))
        {
            var targetState = definition.FindState(selectedTransition.ToStateId);
            var normalizedSource = equipmentSource.Trim().ToLowerInvariant();
            var requiresPurchase = normalizedSource == "purchase";
            var targetsPurchase = targetState.Code == "PURCHASE_PENDING";
            if (requiresPurchase != targetsPurchase)
            {
                return Result.Failure<WorkflowInstanceDto>(
                    requiresPurchase
                        ? "برای خرید تجهیزات، باید گزینه «نیاز به خرید» را انتخاب کنید."
                        : "برای تجهیزات امانی یا موجود، باید گزینه «تجهیزات موجود/امانی» را انتخاب کنید.",
                    "WORKFLOW_EQUIPMENT_SOURCE_MISMATCH");
            }
        }

        var activeTask = instance.Tasks.FirstOrDefault(task =>
            task.Status is WorkflowTaskStatus.Pending or WorkflowTaskStatus.InProgress);
        var effectiveActionKey = state.ActionKey;
        if (activeTask?.ActionKey is not null
            && Enum.TryParse<WorkflowActionKey>(activeTask.ActionKey, true, out var overrideAction))
        {
            effectiveActionKey = overrideAction;
        }

        var context = new WorkflowActionContext(
            order,
            instance,
            definition,
            state,
            transitionId ?? Guid.Empty,
            request.ActionDataJson ?? "{}");

        var actionResult = await ExecuteActionAsync(effectiveActionKey, context, cancellationToken);
        if (actionResult.IsFailure)
        {
            return Result.Failure<WorkflowInstanceDto>(
                actionResult.Errors,
                actionResult.Message);
        }

        // ActivateService is the end-state action and does not transition anywhere.
        if (effectiveActionKey == WorkflowActionKey.ActivateService)
        {
            return await _mediator.Send(
                new GetWorkflowInstanceQuery(instance.Id),
                cancellationToken);
        }

        var transition = await _mediator.Send(
            new ExecuteWorkflowTransitionCommand(
                instance.Id,
                transitionId!.Value,
                null,
                request.ActionDataJson),
            cancellationToken);
        if (transition.IsFailure)
        {
            return transition;
        }

        var target = definition.FindState(transition.Value!.CurrentStateId);
        if (target.ActionKey == WorkflowActionKey.ActivateService)
        {
            var targetContext = context with { State = target };
            var activation = await ExecuteActionAsync(
                target.ActionKey,
                targetContext,
                cancellationToken);
            if (activation.IsFailure)
            {
                return Result.Failure<WorkflowInstanceDto>(
                    activation.Errors,
                    activation.Message);
            }

            return await _mediator.Send(
                new GetWorkflowInstanceQuery(instance.Id),
                cancellationToken);
        }

        return transition;
    }

    private static bool TryReadNeedsVisit(string? actionDataJson, out bool needsVisit)
    {
        needsVisit = false;
        if (string.IsNullOrWhiteSpace(actionDataJson))
        {
            return false;
        }

        try
        {
            using var document = JsonDocument.Parse(actionDataJson);
            if (!document.RootElement.TryGetProperty("needs_visit", out var value))
            {
                return false;
            }

            if (value.ValueKind is JsonValueKind.True or JsonValueKind.False)
            {
                needsVisit = value.GetBoolean();
                return true;
            }

            var text = value.GetString()?.Trim().ToLowerInvariant();
            if (text is "true" or "yes" or "1")
            {
                needsVisit = true;
                return true;
            }

            if (text is "false" or "no" or "0")
            {
                needsVisit = false;
                return true;
            }
        }
        catch (JsonException)
        {
            return false;
        }

        return false;
    }

    private static bool TryReadString(
        string? actionDataJson,
        string propertyName,
        out string value)
    {
        value = string.Empty;
        if (string.IsNullOrWhiteSpace(actionDataJson))
        {
            return false;
        }

        try
        {
            using var document = JsonDocument.Parse(actionDataJson);
            if (!document.RootElement.TryGetProperty(propertyName, out var property)
                || property.ValueKind != JsonValueKind.String)
            {
                return false;
            }

            value = property.GetString() ?? string.Empty;
            return !string.IsNullOrWhiteSpace(value);
        }
        catch (JsonException)
        {
            return false;
        }
    }

    private Task<Result> ExecuteActionAsync(
        WorkflowActionKey key,
        WorkflowActionContext context,
        CancellationToken cancellationToken)
    {
        var action = _actions.FirstOrDefault(item => item.Key == key);
        return action is null
            ? Task.FromResult(Result.Success())
            : action.ExecuteAsync(context, cancellationToken);
    }
}
