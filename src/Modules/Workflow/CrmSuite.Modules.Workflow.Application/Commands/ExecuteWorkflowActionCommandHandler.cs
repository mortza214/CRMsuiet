using CrmSuite.BuildingBlocks.Domain.Results;
using CrmSuite.Modules.Workflow.Application.Actions;
using CrmSuite.Modules.Workflow.Application.Dtos;
using CrmSuite.Modules.Workflow.Application.Queries;
using CrmSuite.Modules.Workflow.Application.Sales;
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
        var context = new WorkflowActionContext(
            order,
            instance,
            definition,
            state,
            request.TransitionId,
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
                request.TransitionId,
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
