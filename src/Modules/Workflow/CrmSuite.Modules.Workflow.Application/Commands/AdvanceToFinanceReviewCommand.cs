using CrmSuite.BuildingBlocks.Domain.Results;
using CrmSuite.Modules.Workflow.Application.Abstractions;
using CrmSuite.Modules.Workflow.Domain.Enums;
using MediatR;

namespace CrmSuite.Modules.Workflow.Application.Commands;

public sealed record AdvanceToFinanceReviewCommand(Guid OrderId)
    : IRequest<Result>;

public sealed class AdvanceToFinanceReviewCommandHandler
    : IRequestHandler<AdvanceToFinanceReviewCommand, Result>
{
    private readonly IWorkflowInstanceRepository _instances;
    private readonly IWorkflowDefinitionRepository _definitions;
    private readonly IMediator _mediator;

    public AdvanceToFinanceReviewCommandHandler(
        IWorkflowInstanceRepository instances,
        IWorkflowDefinitionRepository definitions,
        IMediator mediator)
    {
        _instances = instances;
        _definitions = definitions;
        _mediator = mediator;
    }

    public async Task<Result> Handle(
        AdvanceToFinanceReviewCommand request,
        CancellationToken cancellationToken)
    {
        var instance = await _instances.GetActiveByEntityAsync(
            WorkflowEntityType.Order,
            request.OrderId,
            cancellationToken);
        if (instance is null)
        {
            return Result.Failure(
                "گردش کار فعالی برای این سفارش یافت نشد.",
                "WORKFLOW_ACTIVE_INSTANCE_NOT_FOUND");
        }

        var definition = await _definitions.GetByIdWithGraphAsync(
            instance.DefinitionId,
            cancellationToken);
        if (definition is null)
        {
            return Result.Failure(
                "تعریف گردش کار یافت نشد.",
                "WORKFLOW_DEFINITION_NOT_FOUND");
        }

        var current = definition.FindState(instance.CurrentStateId);
        if (!string.Equals(current.Code, "CUSTOMER_APPROVAL", StringComparison.Ordinal))
        {
            return Result.Failure(
                "گردش کار در مرحله تایید مشتری نیست.",
                "WORKFLOW_NOT_CUSTOMER_APPROVAL");
        }

        var transition = definition.Transitions.FirstOrDefault(item =>
            item.FromStateId == current.Id
            && definition.FindState(item.ToStateId).Code == "FINANCE_REVIEW");
        if (transition is null)
        {
            return Result.Failure(
                "گذار تایید مالی در تعریف گردش کار وجود ندارد.",
                "WORKFLOW_FINANCE_TRANSITION_NOT_FOUND");
        }

        var moved = await _mediator.Send(
            new ExecuteWorkflowTransitionCommand(
                instance.Id,
                transition.Id,
                "ثبت فیش پرداخت و ارسال به بررسی مالی",
                "{}"),
            cancellationToken);
        return moved.IsFailure
            ? Result.Failure(moved.Errors, moved.Message)
            : Result.Success();
    }
}
