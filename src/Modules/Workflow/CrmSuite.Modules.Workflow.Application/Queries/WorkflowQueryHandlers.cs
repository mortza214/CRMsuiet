using CrmSuite.BuildingBlocks.Domain.Results;
using CrmSuite.Modules.Workflow.Application.Abstractions;
using CrmSuite.Modules.Workflow.Application.Dtos;
using CrmSuite.Modules.Workflow.Application.Mapping;
using CrmSuite.Modules.Workflow.Application.Services;
using CrmSuite.Modules.Workflow.Domain.Entities;
using CrmSuite.Modules.Workflow.Domain.Enums;
using MediatR;

namespace CrmSuite.Modules.Workflow.Application.Queries;

public sealed class GetWorkflowDefinitionsQueryHandler
    : IRequestHandler<GetWorkflowDefinitionsQuery, Result<PagedResult<WorkflowDefinitionSummaryDto>>>
{
    private readonly IWorkflowDefinitionRepository _definitions;
    private readonly WorkflowDtoMapper _mapper;

    public GetWorkflowDefinitionsQueryHandler(
        IWorkflowDefinitionRepository definitions,
        WorkflowDtoMapper mapper)
    {
        _definitions = definitions;
        _mapper = mapper;
    }

    public async Task<Result<PagedResult<WorkflowDefinitionSummaryDto>>> Handle(
        GetWorkflowDefinitionsQuery request,
        CancellationToken cancellationToken)
    {
        var (items, total) = await _definitions.GetPagedAsync(
            new WorkflowDefinitionFilter(
                request.Status,
                request.EntityType,
                request.Search,
                Math.Max(1, request.Page),
                Math.Clamp(request.PageSize, 1, 200)),
            cancellationToken);

        return Result.Success(new PagedResult<WorkflowDefinitionSummaryDto>(
            items.Select(_mapper.ToSummary).ToArray(),
            total,
            Math.Max(1, request.Page),
            Math.Clamp(request.PageSize, 1, 200)));
    }
}

public sealed class GetWorkflowDefinitionByIdQueryHandler
    : IRequestHandler<GetWorkflowDefinitionByIdQuery, Result<WorkflowDefinitionDto>>
{
    private readonly IWorkflowDefinitionRepository _definitions;
    private readonly WorkflowDtoMapper _mapper;

    public GetWorkflowDefinitionByIdQueryHandler(
        IWorkflowDefinitionRepository definitions,
        WorkflowDtoMapper mapper)
    {
        _definitions = definitions;
        _mapper = mapper;
    }

    public async Task<Result<WorkflowDefinitionDto>> Handle(
        GetWorkflowDefinitionByIdQuery request,
        CancellationToken cancellationToken)
    {
        var definition = await _definitions.GetByIdWithGraphAsync(request.Id, cancellationToken);
        return definition is null
            ? Result.Failure<WorkflowDefinitionDto>(
                "گردش کار یافت نشد.",
                "WORKFLOW_DEFINITION_NOT_FOUND")
            : Result.Success(_mapper.ToDefinition(definition));
    }
}

public sealed class GetWorkflowInstanceQueryHandler
    : IRequestHandler<GetWorkflowInstanceQuery, Result<WorkflowInstanceDto>>
{
    private readonly IWorkflowDefinitionRepository _definitions;
    private readonly IWorkflowInstanceRepository _instances;
    private readonly WorkflowDtoMapper _mapper;

    public GetWorkflowInstanceQueryHandler(
        IWorkflowDefinitionRepository definitions,
        IWorkflowInstanceRepository instances,
        WorkflowDtoMapper mapper)
    {
        _definitions = definitions;
        _instances = instances;
        _mapper = mapper;
    }

    public async Task<Result<WorkflowInstanceDto>> Handle(
        GetWorkflowInstanceQuery request,
        CancellationToken cancellationToken)
    {
        var instance = await _instances.GetByIdWithGraphAsync(request.Id, cancellationToken);
        if (instance is null)
        {
            return Result.Failure<WorkflowInstanceDto>(
                "نمونه گردش کار یافت نشد.",
                "WORKFLOW_INSTANCE_NOT_FOUND");
        }

        var definition = await _definitions.GetByIdWithGraphAsync(
            instance.DefinitionId,
            cancellationToken);
        return definition is null
            ? Result.Failure<WorkflowInstanceDto>(
                "تعریف گردش کار یافت نشد.",
                "WORKFLOW_DEFINITION_NOT_FOUND")
            : Result.Success(_mapper.ToInstance(instance, definition));
    }
}

public sealed class GetWorkflowInstancesByEntityQueryHandler
    : IRequestHandler<GetWorkflowInstancesByEntityQuery, Result<IReadOnlyList<WorkflowInstanceDto>>>
{
    private readonly IWorkflowDefinitionRepository _definitions;
    private readonly IWorkflowInstanceRepository _instances;
    private readonly WorkflowDtoMapper _mapper;

    public GetWorkflowInstancesByEntityQueryHandler(
        IWorkflowDefinitionRepository definitions,
        IWorkflowInstanceRepository instances,
        WorkflowDtoMapper mapper)
    {
        _definitions = definitions;
        _instances = instances;
        _mapper = mapper;
    }

    public async Task<Result<IReadOnlyList<WorkflowInstanceDto>>> Handle(
        GetWorkflowInstancesByEntityQuery request,
        CancellationToken cancellationToken)
    {
        var instances = await _instances.GetByEntityAsync(
            request.EntityType,
            request.EntityId,
            cancellationToken);

        var result = new List<WorkflowInstanceDto>(instances.Count);
        var definitions = new Dictionary<Guid, WorkflowDefinition>();

        foreach (var instance in instances)
        {
            if (!definitions.TryGetValue(instance.DefinitionId, out var definition))
            {
                definition = await _definitions.GetByIdWithGraphAsync(
                    instance.DefinitionId,
                    cancellationToken);
                if (definition is null)
                {
                    continue;
                }

                definitions[instance.DefinitionId] = definition;
            }

            result.Add(_mapper.ToInstance(instance, definition));
        }

        return Result.Success<IReadOnlyList<WorkflowInstanceDto>>(result);
    }
}

public sealed class GetMyWorkflowTasksQueryHandler
    : IRequestHandler<GetMyWorkflowTasksQuery, Result<PagedResult<WorkflowTaskDto>>>
{
    private readonly IWorkflowInstanceRepository _instances;
    private readonly IWorkflowDefinitionRepository _definitions;
    private readonly IWorkflowActorAccessor _actorAccessor;
    private readonly IWorkflowEntityDisplayResolver? _displayResolver;
    private readonly WorkflowDtoMapper _mapper;

    public GetMyWorkflowTasksQueryHandler(
        IWorkflowInstanceRepository instances,
        IWorkflowDefinitionRepository definitions,
        IWorkflowActorAccessor actorAccessor,
        WorkflowDtoMapper mapper,
        IWorkflowEntityDisplayResolver? displayResolver = null)
    {
        _instances = instances;
        _definitions = definitions;
        _actorAccessor = actorAccessor;
        _displayResolver = displayResolver;
        _mapper = mapper;
    }

    public async Task<Result<PagedResult<WorkflowTaskDto>>> Handle(
        GetMyWorkflowTasksQuery request,
        CancellationToken cancellationToken)
    {
        var actor = await _actorAccessor.GetAsync(cancellationToken);
        if (actor.UserId is null)
        {
            return Result.Failure<PagedResult<WorkflowTaskDto>>(
                "کاربر جاری قابل تشخیص نیست.",
                "WORKFLOW_CURRENT_USER_MISSING");
        }

        var page = Math.Max(1, request.Page);
        var pageSize = Math.Clamp(request.PageSize, 1, 200);
        var (items, total) = await _instances.GetMyTasksAsync(
            actor,
            new WorkflowTaskFilter(request.Status, request.DepartmentId, page, pageSize),
            cancellationToken);

        var definitions = new Dictionary<Guid, WorkflowDefinition>();
        var instances = new Dictionary<Guid, WorkflowInstance>();
        var displays = new Dictionary<(WorkflowEntityType Type, Guid Id), WorkflowEntityDisplay>();
        var result = new List<WorkflowTaskDto>(items.Count);

        foreach (var task in items)
        {
            if (!instances.TryGetValue(task.InstanceId, out var instance))
            {
                instance = await _instances.GetByIdWithGraphAsync(task.InstanceId, cancellationToken);
                if (instance is null)
                {
                    continue;
                }

                instances[task.InstanceId] = instance;
            }

            if (!definitions.TryGetValue(instance.DefinitionId, out var definition))
            {
                definition = await _definitions.GetByIdWithGraphAsync(
                    instance.DefinitionId,
                    cancellationToken);
                if (definition is null)
                {
                    continue;
                }

                definitions[instance.DefinitionId] = definition;
            }

            var key = (instance.EntityType, instance.EntityId);
            if (_displayResolver is null)
            {
                result.Add(_mapper.ToTask(task, instance, definition));
                continue;
            }

            if (!displays.TryGetValue(key, out var display))
            {
                display = await _displayResolver.ResolveAsync(
                    instance.EntityType,
                    instance.EntityId,
                    cancellationToken);
                displays[key] = display;
            }

            result.Add(_mapper.ToTask(task, instance, definition) with
            {
                EntityDisplayName = display.DisplayName,
                EntityReference = display.Reference,
            });
        }

        return Result.Success(new PagedResult<WorkflowTaskDto>(
            result,
            total,
            page,
            pageSize));
    }
}
