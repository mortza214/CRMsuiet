using CrmSuite.BuildingBlocks.Domain.Results;
using CrmSuite.Modules.Workflow.Application.Sales;
using CrmSuite.Modules.Workflow.Domain.Enums;

namespace CrmSuite.Modules.Workflow.Application.Actions;

public sealed class ReviewFeasibilityAction : IWorkflowAction
{
    private readonly BandwidthPhase6Support _support;

    public ReviewFeasibilityAction(BandwidthPhase6Support support)
    {
        _support = support;
    }

    public WorkflowActionKey Key => WorkflowActionKey.ReviewFeasibility;

    public async Task<Result> ExecuteAsync(
        WorkflowActionContext context,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(context.ActionDataJson)
            || context.ActionDataJson == "{}")
        {
            return Result.Success();
        }

        var submitted = await _support.SubmitFormAsync(
            context.State,
            context.Order,
            context.ActionDataJson,
            cancellationToken);
        return submitted.IsFailure
            ? Result.Failure(submitted.Errors, submitted.Message)
            : Result.Success();
    }
}
