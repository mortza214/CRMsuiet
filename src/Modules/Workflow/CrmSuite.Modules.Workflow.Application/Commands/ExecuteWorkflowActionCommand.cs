using CrmSuite.BuildingBlocks.Domain.Results;
using CrmSuite.Modules.Workflow.Application.Dtos;
using MediatR;

namespace CrmSuite.Modules.Workflow.Application.Commands;

public sealed record ExecuteWorkflowActionCommand(
    Guid InstanceId,
    Guid? TransitionId = null,
    string? ActionDataJson = null) : IRequest<Result<WorkflowInstanceDto>>;
