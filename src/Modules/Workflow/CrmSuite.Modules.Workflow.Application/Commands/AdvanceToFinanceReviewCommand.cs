using CrmSuite.BuildingBlocks.Domain.Results;
using CrmSuite.Modules.Workflow.Application.Abstractions;
using CrmSuite.Modules.Workflow.Domain.Entities;
using CrmSuite.Modules.Workflow.Domain.Enums;
using CrmSuite.Modules.Identity.Application.Abstractions;
using CrmSuite.BuildingBlocks.Application.Abstractions;
using FoundationCurrentUser = CrmSuite.Modules.Foundation.Application.Abstractions.ICurrentUserService;
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
    private readonly IWorkflowUnitOfWork _unitOfWork;
    private readonly IRoleRepository _roles;
    private readonly IDateTimeProvider _clock;
    private readonly FoundationCurrentUser _currentUser;

    public AdvanceToFinanceReviewCommandHandler(
        IWorkflowInstanceRepository instances,
        IWorkflowDefinitionRepository definitions,
        IMediator mediator,
        IWorkflowUnitOfWork unitOfWork,
        IRoleRepository roles,
        IDateTimeProvider clock,
        FoundationCurrentUser currentUser)
    {
        _instances = instances;
        _definitions = definitions;
        _mediator = mediator;
        _unitOfWork = unitOfWork;
        _roles = roles;
        _clock = clock;
        _currentUser = currentUser;
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
            return await CreateFallbackTaskAsync(instance, current, cancellationToken);
        }

        var moved = await _mediator.Send(
            new ExecuteWorkflowTransitionCommand(
                instance.Id,
                transition.Id,
                "ثبت فیش پرداخت و ارسال به بررسی مالی",
                "{}"),
            cancellationToken);
        if (moved.IsFailure)
        {
            return await CreateFallbackTaskAsync(instance, current, cancellationToken);
        }

        return Result.Success();
    }

    private async Task<Result> CreateFallbackTaskAsync(
        WorkflowInstance instance,
        WorkflowState current,
        CancellationToken cancellationToken)
    {
        var roles = await _roles.GetAllAsync(includePermissions: false, cancellationToken);
        var financeRole = roles.FirstOrDefault(role => role.Name == "FinanceExpert");
        if (financeRole is null)
        {
            return Result.Failure(
                "گذار تایید مالی و نقش کارشناس مالی در دسترس نیست.",
                "WORKFLOW_FINANCE_FALLBACK_UNAVAILABLE");
        }

        if (!instance.Tasks.Any(task =>
                task.Status == WorkflowTaskStatus.Pending
                && task.AssignedRoleId == financeRole.Id))
        {
            var fallbackTask = new WorkflowTask(
                instance.Id,
                current.Id,
                null,
                financeRole.Id,
                null,
                null,
                _currentUser.UserId,
                _clock.UtcNow);
            fallbackTask.SetActionOverride("VerifyPayment", null);
            instance.AddTask(fallbackTask);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }

        return Result.Success("تسک بررسی مالی به صورت جایگزین ایجاد شد.");
    }
}
