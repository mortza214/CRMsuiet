using CrmSuite.BuildingBlocks.Application.Abstractions;
using CrmSuite.BuildingBlocks.Domain.Exceptions;
using CrmSuite.BuildingBlocks.Domain.Results;
using CrmSuite.Modules.Catalog.Application.Abstractions;
using CrmSuite.Modules.Foundation.Application.Abstractions;
using CrmSuite.Modules.Foundation.Application.Auditing;
using CrmSuite.Modules.Foundation.Domain.Enums;
using CrmSuite.Modules.Workflow.Application.Abstractions;
using CrmSuite.Modules.Workflow.Application.Dtos;
using CrmSuite.Modules.Workflow.Application.Mapping;
using CrmSuite.Modules.Workflow.Domain.Entities;
using CrmSuite.Modules.Workflow.Domain.Enums;
using MediatR;
using FoundationCurrentUser = CrmSuite.Modules.Foundation.Application.Abstractions.ICurrentUserService;

namespace CrmSuite.Modules.Workflow.Application.Commands;

public sealed class CreateWorkflowDefinitionCommandHandler
    : IRequestHandler<CreateWorkflowDefinitionCommand, Result<WorkflowDefinitionDto>>
{
    private readonly IWorkflowDefinitionRepository _definitions;
    private readonly IWorkflowUnitOfWork _unitOfWork;
    private readonly IAuditScope _auditScope;
    private readonly FoundationCurrentUser _currentUser;
    private readonly IDateTimeProvider _clock;
    private readonly WorkflowDtoMapper _mapper;

    public CreateWorkflowDefinitionCommandHandler(
        IWorkflowDefinitionRepository definitions,
        IWorkflowUnitOfWork unitOfWork,
        IAuditScope auditScope,
        FoundationCurrentUser currentUser,
        IDateTimeProvider clock,
        WorkflowDtoMapper mapper)
    {
        _definitions = definitions;
        _unitOfWork = unitOfWork;
        _auditScope = auditScope;
        _currentUser = currentUser;
        _clock = clock;
        _mapper = mapper;
    }

    public async Task<Result<WorkflowDefinitionDto>> Handle(
        CreateWorkflowDefinitionCommand request,
        CancellationToken cancellationToken)
    {
        try
        {
            if (await _definitions.CodeExistsAsync(request.Code, null, cancellationToken))
            {
                return Result.Failure<WorkflowDefinitionDto>(
                    "کد گردش کار قبلاً استفاده شده است.",
                    "WORKFLOW_DEFINITION_CODE_DUPLICATE");
            }

            var definition = new WorkflowDefinition(
                request.Code,
                request.Name,
                request.EntityType,
                request.Description,
                _currentUser.UserId,
                _clock.UtcNow);

            _definitions.Add(definition);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            _auditScope.EntityId = definition.Id.ToString();

            return Result.Success(_mapper.ToDefinition(definition));
        }
        catch (DomainException exception)
        {
            return Result.Failure<WorkflowDefinitionDto>(exception.Message, exception.Code);
        }
    }
}

public sealed class UpdateWorkflowDraftCommandHandler
    : IRequestHandler<UpdateWorkflowDraftCommand, Result<WorkflowDefinitionDto>>
{
    private readonly IWorkflowDefinitionRepository _definitions;
    private readonly IWorkflowUnitOfWork _unitOfWork;
    private readonly IAuditScope _auditScope;
    private readonly FoundationCurrentUser _currentUser;
    private readonly IDateTimeProvider _clock;
    private readonly WorkflowDtoMapper _mapper;

    public UpdateWorkflowDraftCommandHandler(
        IWorkflowDefinitionRepository definitions,
        IWorkflowUnitOfWork unitOfWork,
        IAuditScope auditScope,
        FoundationCurrentUser currentUser,
        IDateTimeProvider clock,
        WorkflowDtoMapper mapper)
    {
        _definitions = definitions;
        _unitOfWork = unitOfWork;
        _auditScope = auditScope;
        _currentUser = currentUser;
        _clock = clock;
        _mapper = mapper;
    }

    public async Task<Result<WorkflowDefinitionDto>> Handle(
        UpdateWorkflowDraftCommand request,
        CancellationToken cancellationToken)
    {
        var definition = await _definitions.GetByIdWithGraphAsync(request.DefinitionId, cancellationToken);
        if (definition is null)
        {
            return Result.Failure<WorkflowDefinitionDto>(
                "گردش کار یافت نشد.",
                "WORKFLOW_DEFINITION_NOT_FOUND");
        }

        try
        {
            definition.UpdateDraft(
                request.Name,
                request.Description,
                _currentUser.UserId,
                _clock.UtcNow);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            _auditScope.EntityId = definition.Id.ToString();
            return Result.Success(_mapper.ToDefinition(definition));
        }
        catch (DomainException exception)
        {
            return Result.Failure<WorkflowDefinitionDto>(exception.Message, exception.Code);
        }
    }
}

public sealed class PublishWorkflowDefinitionCommandHandler
    : IRequestHandler<PublishWorkflowDefinitionCommand, Result<WorkflowDefinitionDto>>
{
    private readonly IWorkflowDefinitionRepository _definitions;
    private readonly IWorkflowUnitOfWork _unitOfWork;
    private readonly IAuditScope _auditScope;
    private readonly FoundationCurrentUser _currentUser;
    private readonly IDateTimeProvider _clock;
    private readonly WorkflowDtoMapper _mapper;

    public PublishWorkflowDefinitionCommandHandler(
        IWorkflowDefinitionRepository definitions,
        IWorkflowUnitOfWork unitOfWork,
        IAuditScope auditScope,
        FoundationCurrentUser currentUser,
        IDateTimeProvider clock,
        WorkflowDtoMapper mapper)
    {
        _definitions = definitions;
        _unitOfWork = unitOfWork;
        _auditScope = auditScope;
        _currentUser = currentUser;
        _clock = clock;
        _mapper = mapper;
    }

    public async Task<Result<WorkflowDefinitionDto>> Handle(
        PublishWorkflowDefinitionCommand request,
        CancellationToken cancellationToken)
    {
        var definition = await _definitions.GetByIdWithGraphAsync(request.DefinitionId, cancellationToken);
        if (definition is null)
        {
            return Result.Failure<WorkflowDefinitionDto>(
                "گردش کار یافت نشد.",
                "WORKFLOW_DEFINITION_NOT_FOUND");
        }

        try
        {
            definition.Publish(_currentUser.UserId, _clock.UtcNow);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            _auditScope.EntityId = definition.Id.ToString();
            return Result.Success(_mapper.ToDefinition(definition));
        }
        catch (DomainException exception)
        {
            return Result.Failure<WorkflowDefinitionDto>(exception.Message, exception.Code);
        }
    }
}

public sealed class ArchiveWorkflowDefinitionCommandHandler
    : IRequestHandler<ArchiveWorkflowDefinitionCommand, Result<WorkflowDefinitionDto>>
{
    private readonly IWorkflowDefinitionRepository _definitions;
    private readonly IWorkflowUnitOfWork _unitOfWork;
    private readonly IAuditScope _auditScope;
    private readonly FoundationCurrentUser _currentUser;
    private readonly IDateTimeProvider _clock;
    private readonly WorkflowDtoMapper _mapper;

    public ArchiveWorkflowDefinitionCommandHandler(
        IWorkflowDefinitionRepository definitions,
        IWorkflowUnitOfWork unitOfWork,
        IAuditScope auditScope,
        FoundationCurrentUser currentUser,
        IDateTimeProvider clock,
        WorkflowDtoMapper mapper)
    {
        _definitions = definitions;
        _unitOfWork = unitOfWork;
        _auditScope = auditScope;
        _currentUser = currentUser;
        _clock = clock;
        _mapper = mapper;
    }

    public async Task<Result<WorkflowDefinitionDto>> Handle(
        ArchiveWorkflowDefinitionCommand request,
        CancellationToken cancellationToken)
    {
        var definition = await _definitions.GetByIdWithGraphAsync(request.DefinitionId, cancellationToken);
        if (definition is null)
        {
            return Result.Failure<WorkflowDefinitionDto>(
                "گردش کار یافت نشد.",
                "WORKFLOW_DEFINITION_NOT_FOUND");
        }

        try
        {
            definition.Archive(_currentUser.UserId, _clock.UtcNow);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            _auditScope.EntityId = definition.Id.ToString();
            return Result.Success(_mapper.ToDefinition(definition));
        }
        catch (DomainException exception)
        {
            return Result.Failure<WorkflowDefinitionDto>(exception.Message, exception.Code);
        }
    }
}

public sealed class AddWorkflowStateCommandHandler
    : IRequestHandler<AddWorkflowStateCommand, Result<WorkflowStateDto>>
{
    private readonly IWorkflowDefinitionRepository _definitions;
    private readonly IWorkflowUnitOfWork _unitOfWork;
    private readonly IAuditScope _auditScope;
    private readonly FoundationCurrentUser _currentUser;
    private readonly IDateTimeProvider _clock;
    private readonly WorkflowDtoMapper _mapper;

    public AddWorkflowStateCommandHandler(
        IWorkflowDefinitionRepository definitions,
        IWorkflowUnitOfWork unitOfWork,
        IAuditScope auditScope,
        FoundationCurrentUser currentUser,
        IDateTimeProvider clock,
        WorkflowDtoMapper mapper)
    {
        _definitions = definitions;
        _unitOfWork = unitOfWork;
        _auditScope = auditScope;
        _currentUser = currentUser;
        _clock = clock;
        _mapper = mapper;
    }

    public async Task<Result<WorkflowStateDto>> Handle(
        AddWorkflowStateCommand request,
        CancellationToken cancellationToken)
    {
        var definition = await _definitions.GetByIdWithGraphAsync(request.DefinitionId, cancellationToken);
        if (definition is null)
        {
            return Result.Failure<WorkflowStateDto>(
                "گردش کار یافت نشد.",
                "WORKFLOW_DEFINITION_NOT_FOUND");
        }

        try
        {
            var state = definition.AddState(
                request.Code,
                request.Name,
                request.StateType,
                request.Order,
                request.IsFinal,
                request.AssigneeType,
                request.AssigneeValue,
                request.FormCode,
                request.Color,
                request.ActionKey);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            _auditScope.EntityId = definition.Id.ToString();
            return Result.Success(_mapper.ToState(state));
        }
        catch (DomainException exception)
        {
            return Result.Failure<WorkflowStateDto>(exception.Message, exception.Code);
        }
    }
}

public sealed class UpdateWorkflowStateCommandHandler
    : IRequestHandler<UpdateWorkflowStateCommand, Result<WorkflowStateDto>>
{
    private readonly IWorkflowDefinitionRepository _definitions;
    private readonly IWorkflowUnitOfWork _unitOfWork;
    private readonly IAuditScope _auditScope;
    private readonly FoundationCurrentUser _currentUser;
    private readonly IDateTimeProvider _clock;
    private readonly WorkflowDtoMapper _mapper;

    public UpdateWorkflowStateCommandHandler(
        IWorkflowDefinitionRepository definitions,
        IWorkflowUnitOfWork unitOfWork,
        IAuditScope auditScope,
        FoundationCurrentUser currentUser,
        IDateTimeProvider clock,
        WorkflowDtoMapper mapper)
    {
        _definitions = definitions;
        _unitOfWork = unitOfWork;
        _auditScope = auditScope;
        _currentUser = currentUser;
        _clock = clock;
        _mapper = mapper;
    }

    public async Task<Result<WorkflowStateDto>> Handle(
        UpdateWorkflowStateCommand request,
        CancellationToken cancellationToken)
    {
        var definition = await _definitions.GetByIdWithGraphAsync(request.DefinitionId, cancellationToken);
        if (definition is null)
        {
            return Result.Failure<WorkflowStateDto>(
                "گردش کار یافت نشد.",
                "WORKFLOW_DEFINITION_NOT_FOUND");
        }

        try
        {
            definition.UpdateState(
                request.StateId,
                request.Code,
                request.Name,
                request.StateType,
                request.Order,
                request.IsFinal,
                request.AssigneeType,
                request.AssigneeValue,
                request.FormCode,
                request.Color,
                _currentUser.UserId,
                _clock.UtcNow,
                request.ActionKey);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            _auditScope.EntityId = definition.Id.ToString();
            return Result.Success(_mapper.ToState(definition.FindState(request.StateId)));
        }
        catch (DomainException exception)
        {
            return Result.Failure<WorkflowStateDto>(exception.Message, exception.Code);
        }
    }
}

public sealed class RemoveWorkflowStateCommandHandler
    : IRequestHandler<RemoveWorkflowStateCommand, Result>
{
    private readonly IWorkflowDefinitionRepository _definitions;
    private readonly IWorkflowUnitOfWork _unitOfWork;
    private readonly IAuditScope _auditScope;
    private readonly FoundationCurrentUser _currentUser;
    private readonly IDateTimeProvider _clock;

    public RemoveWorkflowStateCommandHandler(
        IWorkflowDefinitionRepository definitions,
        IWorkflowUnitOfWork unitOfWork,
        IAuditScope auditScope,
        FoundationCurrentUser currentUser,
        IDateTimeProvider clock)
    {
        _definitions = definitions;
        _unitOfWork = unitOfWork;
        _auditScope = auditScope;
        _currentUser = currentUser;
        _clock = clock;
    }

    public async Task<Result> Handle(
        RemoveWorkflowStateCommand request,
        CancellationToken cancellationToken)
    {
        var definition = await _definitions.GetByIdWithGraphAsync(request.DefinitionId, cancellationToken);
        if (definition is null)
        {
            return Result.Failure("گردش کار یافت نشد.", "WORKFLOW_DEFINITION_NOT_FOUND");
        }

        try
        {
            definition.RemoveState(request.StateId, _currentUser.UserId, _clock.UtcNow);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            _auditScope.EntityId = definition.Id.ToString();
            return Result.Success();
        }
        catch (DomainException exception)
        {
            return Result.Failure(exception.Message, exception.Code);
        }
    }
}

public sealed class AddWorkflowTransitionCommandHandler
    : IRequestHandler<AddWorkflowTransitionCommand, Result<WorkflowTransitionDto>>
{
    private readonly IWorkflowDefinitionRepository _definitions;
    private readonly IWorkflowUnitOfWork _unitOfWork;
    private readonly IAuditScope _auditScope;
    private readonly FoundationCurrentUser _currentUser;
    private readonly IDateTimeProvider _clock;
    private readonly WorkflowDtoMapper _mapper;

    public AddWorkflowTransitionCommandHandler(
        IWorkflowDefinitionRepository definitions,
        IWorkflowUnitOfWork unitOfWork,
        IAuditScope auditScope,
        FoundationCurrentUser currentUser,
        IDateTimeProvider clock,
        WorkflowDtoMapper mapper)
    {
        _definitions = definitions;
        _unitOfWork = unitOfWork;
        _auditScope = auditScope;
        _currentUser = currentUser;
        _clock = clock;
        _mapper = mapper;
    }

    public async Task<Result<WorkflowTransitionDto>> Handle(
        AddWorkflowTransitionCommand request,
        CancellationToken cancellationToken)
    {
        var definition = await _definitions.GetByIdWithGraphAsync(request.DefinitionId, cancellationToken);
        if (definition is null)
        {
            return Result.Failure<WorkflowTransitionDto>(
                "گردش کار یافت نشد.",
                "WORKFLOW_DEFINITION_NOT_FOUND");
        }

        try
        {
            var transition = definition.AddTransition(
                request.FromStateId,
                request.ToStateId,
                request.Name,
                request.GuardType,
                request.RequiredPermission,
                request.GuardPluginKey,
                request.RequiresComment,
                request.SortOrder,
                _currentUser.UserId,
                _clock.UtcNow);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            _auditScope.EntityId = definition.Id.ToString();
            return Result.Success(_mapper.ToTransition(transition));
        }
        catch (DomainException exception)
        {
            return Result.Failure<WorkflowTransitionDto>(exception.Message, exception.Code);
        }
    }
}

public sealed class RemoveWorkflowTransitionCommandHandler
    : IRequestHandler<RemoveWorkflowTransitionCommand, Result>
{
    private readonly IWorkflowDefinitionRepository _definitions;
    private readonly IWorkflowUnitOfWork _unitOfWork;
    private readonly IAuditScope _auditScope;
    private readonly FoundationCurrentUser _currentUser;
    private readonly IDateTimeProvider _clock;

    public RemoveWorkflowTransitionCommandHandler(
        IWorkflowDefinitionRepository definitions,
        IWorkflowUnitOfWork unitOfWork,
        IAuditScope auditScope,
        FoundationCurrentUser currentUser,
        IDateTimeProvider clock)
    {
        _definitions = definitions;
        _unitOfWork = unitOfWork;
        _auditScope = auditScope;
        _currentUser = currentUser;
        _clock = clock;
    }

    public async Task<Result> Handle(
        RemoveWorkflowTransitionCommand request,
        CancellationToken cancellationToken)
    {
        var definition = await _definitions.GetByIdWithGraphAsync(request.DefinitionId, cancellationToken);
        if (definition is null)
        {
            return Result.Failure("گردش کار یافت نشد.", "WORKFLOW_DEFINITION_NOT_FOUND");
        }

        try
        {
            definition.RemoveTransition(request.TransitionId, _currentUser.UserId, _clock.UtcNow);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            _auditScope.EntityId = definition.Id.ToString();
            return Result.Success();
        }
        catch (DomainException exception)
        {
            return Result.Failure(exception.Message, exception.Code);
        }
    }
}

public sealed class BindServiceWorkflowCommandHandler
    : IRequestHandler<BindServiceWorkflowCommand, Result>
{
    private readonly IWorkflowDefinitionRepository _definitions;
    private readonly IServiceRepository _services;
    private readonly ICatalogUnitOfWork _catalogUnitOfWork;
    private readonly IAuditScope _auditScope;
    private readonly IAuditService _auditService;
    private readonly FoundationCurrentUser _currentUser;
    private readonly IDateTimeProvider _clock;

    public BindServiceWorkflowCommandHandler(
        IWorkflowDefinitionRepository definitions,
        IServiceRepository services,
        ICatalogUnitOfWork catalogUnitOfWork,
        IAuditScope auditScope,
        IAuditService auditService,
        FoundationCurrentUser currentUser,
        IDateTimeProvider clock)
    {
        _definitions = definitions;
        _services = services;
        _catalogUnitOfWork = catalogUnitOfWork;
        _auditScope = auditScope;
        _auditService = auditService;
        _currentUser = currentUser;
        _clock = clock;
    }

    public async Task<Result> Handle(
        BindServiceWorkflowCommand request,
        CancellationToken cancellationToken)
    {
        var definition = await _definitions.GetByIdWithGraphAsync(
            request.DefinitionId,
            cancellationToken);
        if (definition is null)
        {
            return Result.Failure("گردش کار یافت نشد.", "WORKFLOW_DEFINITION_NOT_FOUND");
        }

        if (definition.Status != WorkflowDefinitionStatus.Published)
        {
            return Result.Failure("فقط گردش کار منتشرشده قابل اتصال به سرویس است.", "WORKFLOW_DEFINITION_NOT_PUBLISHED");
        }

        var service = await _services.GetByIdAsync(request.ServiceId, cancellationToken);
        if (service is null)
        {
            return Result.Failure("سرویس یافت نشد.", "CATALOG_SERVICE_NOT_FOUND");
        }

        service.SetWorkflowDefinition(definition.Id, _currentUser.UserId, _clock.UtcNow);
        await _catalogUnitOfWork.SaveChangesAsync(cancellationToken);
        _auditScope.EntityId = service.Id.ToString();

        await _auditService.RecordAsync(
            new AuditEntry(
                "CatalogService",
                service.Id.ToString(),
                AuditAction.Update,
                new { WorkflowDefinitionId = (Guid?)null },
                new { WorkflowDefinitionId = definition.Id, DefinitionCode = definition.Code },
                _currentUser.UserId),
            cancellationToken);

        return Result.Success();
    }
}