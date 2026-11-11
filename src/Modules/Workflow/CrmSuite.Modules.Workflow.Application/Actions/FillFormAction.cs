using CrmSuite.BuildingBlocks.Domain.Results;
using CrmSuite.Modules.Workflow.Application.Sales;
using CrmSuite.Modules.Workflow.Domain.Enums;

namespace CrmSuite.Modules.Workflow.Application.Actions;

public sealed class FillFormAction : IWorkflowAction
{
    private readonly BandwidthPhase6Support _support;

    public FillFormAction(BandwidthPhase6Support support)
    {
        _support = support;
    }

    public WorkflowActionKey Key => WorkflowActionKey.FillForm;

    public async Task<Result> ExecuteAsync(
        WorkflowActionContext context,
        CancellationToken cancellationToken)
    {
        var result = await _support.SubmitFormAsync(
            context.State,
            context.Order,
            context.ActionDataJson,
            cancellationToken);
        return result.IsFailure ? Result.Failure(result.Errors, result.Message) : Result.Success();
    }
}
