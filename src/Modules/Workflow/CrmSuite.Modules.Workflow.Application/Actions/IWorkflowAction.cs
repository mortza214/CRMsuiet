using CrmSuite.BuildingBlocks.Domain.Results;
using CrmSuite.Modules.Orders.Domain.Entities;
using CrmSuite.Modules.Workflow.Domain.Entities;
using CrmSuite.Modules.Workflow.Domain.Enums;

namespace CrmSuite.Modules.Workflow.Application.Actions;

public sealed record WorkflowActionContext(
    Order Order,
    WorkflowInstance Instance,
    WorkflowDefinition Definition,
    WorkflowState State,
    Guid TransitionId,
    string ActionDataJson);

public interface IWorkflowAction
{
    WorkflowActionKey Key { get; }

    Task<Result> ExecuteAsync(
        WorkflowActionContext context,
        CancellationToken cancellationToken);
}
