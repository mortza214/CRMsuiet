using CrmSuite.BuildingBlocks.Domain.Results;
using CrmSuite.Modules.Workflow.Application.Abstractions;
using CrmSuite.Modules.Workflow.Application.Dtos;
using CrmSuite.Modules.Workflow.Domain.Enums;
using MediatR;

namespace CrmSuite.Modules.Workflow.Application.Queries;

public sealed record GetWorkflowDefinitionsQuery(
    int Page = 1,
    int PageSize = 20,
    WorkflowDefinitionStatus? Status = null,
    WorkflowEntityType? EntityType = null,
    string? Search = null)
    : IRequest<Result<PagedResult<WorkflowDefinitionSummaryDto>>>;

public sealed record GetWorkflowDefinitionByIdQuery(Guid Id)
    : IRequest<Result<WorkflowDefinitionDto>>;

public sealed record GetWorkflowInstanceQuery(Guid Id)
    : IRequest<Result<WorkflowInstanceDto>>;

public sealed record GetWorkflowInstancesByEntityQuery(
    WorkflowEntityType EntityType,
    Guid EntityId)
    : IRequest<Result<IReadOnlyList<WorkflowInstanceDto>>>;

public sealed record GetMyWorkflowTasksQuery(
    WorkflowTaskStatus? Status = null,
    Guid? DepartmentId = null,
    int Page = 1,
    int PageSize = 20)
    : IRequest<Result<PagedResult<WorkflowTaskDto>>>;
