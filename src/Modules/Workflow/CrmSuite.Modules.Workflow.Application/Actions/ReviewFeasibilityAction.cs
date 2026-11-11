using CrmSuite.BuildingBlocks.Domain.Results;
using CrmSuite.Modules.Workflow.Domain.Enums;

namespace CrmSuite.Modules.Workflow.Application.Actions;

public sealed class ReviewFeasibilityAction : IWorkflowAction
{
    public WorkflowActionKey Key => WorkflowActionKey.ReviewFeasibility;

    public Task<Result> ExecuteAsync(
        WorkflowActionContext context,
        CancellationToken cancellationToken) =>
        Task.FromResult(Result.Success());
}
