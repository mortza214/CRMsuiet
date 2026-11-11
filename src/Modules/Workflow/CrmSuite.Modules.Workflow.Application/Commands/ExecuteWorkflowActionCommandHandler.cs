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
                           ?? ChooseAutomaticTransition(
                               state,
                               definition,
                               request.ActionDataJson ?? "{}");
        if (!transitionId.HasValue
            && state.ActionKey != WorkflowActionKey.ActivateService)
        {
            return Result.Failure<WorkflowInstanceDto>(
                "گذاری برای این مرحله تعریف نشده است.",
                "WORKFLOW_TRANSITION_NOT_FOUND");
        }

        var context = new WorkflowActionContext(
            order,
            instance,
            definition,
            state,
            transitionId ?? Guid.Empty,
            request.ActionDataJson ?? "{}");

        var actionResult = await ExecuteActionAsync(state.ActionKey, context, cancellationToken);
        if (actionResult.IsFailure)
        {
            return Result.Failure<WorkflowInstanceDto>(
                actionResult.Errors,
                actionResult.Message);
        }

        // ActivateService is the end-state action and does not transition anywhere.
        if (state.ActionKey == WorkflowActionKey.ActivateService)
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

    private static Guid? ChooseAutomaticTransition(
        WorkflowState state,
        WorkflowDefinition definition,
        string actionData)
    {
        var transitions = definition.Transitions
            .Where(item => item.FromStateId == state.Id)
            .OrderBy(item => item.SortOrder)
            .ToArray();
        if (transitions.Length <= 1)
        {
            return transitions.FirstOrDefault()?.Id;
        }

        using var document = JsonDocument.Parse(
            string.IsNullOrWhiteSpace(actionData) ? "{}" : actionData);
        var root = document.RootElement;

        var targetCode = state.Code switch
        {
            "INITIAL_REQUEST" => root.TryGetProperty("substrate", out var substrate)
                                 && substrate.ValueKind == JsonValueKind.String
                                 && substrate.GetString() is "fiber" or "cable"
                ? "EQUIPMENT_SUPPLY"
                : "FEASIBILITY_PENDING",
            "EQUIPMENT_SUPPLY" => root.TryGetProperty("mode", out var equipmentMode)
                                  && equipmentMode.ValueKind == JsonValueKind.String
                                  && equipmentMode.GetString() == "Purchase"
                ? "PURCHASE_PENDING"
                : "PROFORMA",
            "CUSTOMER_APPROVAL" => root.TryGetProperty("decision", out var decision)
                                   && decision.ValueKind == JsonValueKind.String
                                   && decision.GetString() == "cancel"
                ? "CANCELLED"
                : "INSTALLATION_PENDING",
            "PAYMENT_PENDING" => root.TryGetProperty("paymentMode", out var paymentMode)
                                 && paymentMode.ValueKind == JsonValueKind.String
                                 && paymentMode.GetString() == "ManualDeposit"
                ? "FINANCE_REVIEW"
                : root.TryGetProperty("cooperationType", out var cooperation)
                  && cooperation.ValueKind == JsonValueKind.String
                  && cooperation.GetString() == "contract"
                    ? "CONTRACT"
                    : "COMPLETED",
            "FINANCE_REVIEW" => root.TryGetProperty("approved", out var approved)
                                && approved.ValueKind == JsonValueKind.True
                ? "CONTRACT"
                : "PAYMENT_PENDING",
            _ => null,
        };

        return transitions
                   .FirstOrDefault(item =>
                       item.ToStateId == definition.States
                           .FirstOrDefault(target => target.Code == targetCode)?.Id)
                   ?.Id
               ?? transitions[0].Id;
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
