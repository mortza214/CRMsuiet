using CrmSuite.BuildingBlocks.Domain.Results;
using CrmSuite.Modules.Workflow.Application.Dtos;
using CrmSuite.Modules.Workflow.Domain.Enums;
using MediatR;

namespace CrmSuite.Modules.Workflow.Application.Commands;

public sealed record StartWorkflowCommand(
    WorkflowEntityType EntityType,
    Guid EntityId,
    string? DefinitionCode,
    string? InitialPayload = null,
    string? IdempotencyKey = null) : IRequest<Result<WorkflowInstanceDto>>;

public sealed record ExecuteWorkflowTransitionCommand(
    Guid InstanceId,
    Guid TransitionId,
    string? Comment = null,
    string? PayloadJson = null,
    string? IdempotencyKey = null) : IRequest<Result<WorkflowInstanceDto>>;

public sealed record CancelWorkflowInstanceCommand(
    Guid InstanceId,
    string? Reason = null) : IRequest<Result<WorkflowInstanceDto>>;

public sealed record ReassignWorkflowTaskCommand(
    Guid TaskId,
    Guid? UserId,
    Guid? RoleId,
    Guid? DepartmentId) : IRequest<Result<WorkflowTaskDto>>;
