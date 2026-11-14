using System.Text.Json;
using CrmSuite.BuildingBlocks.Application.Abstractions;
using CrmSuite.BuildingBlocks.Application.Security;
using CrmSuite.BuildingBlocks.Domain.Exceptions;
using CrmSuite.BuildingBlocks.Domain.Results;
using CrmSuite.Modules.Foundation.Application.Abstractions;
using CrmSuite.Modules.Foundation.Application.Auditing;
using CrmSuite.Modules.Foundation.Domain.Enums;
using CrmSuite.Modules.Workflow.Application.Abstractions;
using CrmSuite.Modules.Workflow.Application.Dtos;
using CrmSuite.Modules.Workflow.Application.Mapping;
using CrmSuite.Modules.Workflow.Application.Services;
using CrmSuite.Modules.Workflow.Domain.Entities;
using CrmSuite.Modules.Workflow.Domain.Enums;
using MediatR;
using CrmSuite.Modules.Workflow.Application.Sales;
using CrmSuite.Modules.Orders.Application.Abstractions;
using FoundationCurrentUser = CrmSuite.Modules.Foundation.Application.Abstractions.ICurrentUserService;

namespace CrmSuite.Modules.Workflow.Application.Commands;

public sealed class StartWorkflowCommandHandler
    : IRequestHandler<StartWorkflowCommand, Result<WorkflowInstanceDto>>
{
    private readonly IWorkflowDefinitionRepository _definitions;
    private readonly IWorkflowInstanceRepository _instances;
    private readonly IWorkflowUnitOfWork _unitOfWork;
    private readonly IWorkflowActorAccessor _actorAccessor;
    private readonly IWorkflowEntityCustomerResolver _customerResolver;
    private readonly WorkflowAssignmentResolver _assignmentResolver;
    private readonly IWorkflowEntityLinker _entityLinker;
    private readonly IWorkflowNotifier _notifier;
    private readonly IAuditScope _auditScope;
    private readonly IAuditService _auditService;
    private readonly FoundationCurrentUser _currentUser;
    private readonly IDateTimeProvider _clock;
    private readonly WorkflowDtoMapper _mapper;

    public StartWorkflowCommandHandler(
        IWorkflowDefinitionRepository definitions,
        IWorkflowInstanceRepository instances,
        IWorkflowUnitOfWork unitOfWork,
        IWorkflowActorAccessor actorAccessor,
        IWorkflowEntityCustomerResolver customerResolver,
        WorkflowAssignmentResolver assignmentResolver,
        IWorkflowEntityLinker entityLinker,
        IWorkflowNotifier notifier,
        IAuditScope auditScope,
        IAuditService auditService,
        FoundationCurrentUser currentUser,
        IDateTimeProvider clock,
        WorkflowDtoMapper mapper)
    {
        _definitions = definitions;
        _instances = instances;
        _unitOfWork = unitOfWork;
        _actorAccessor = actorAccessor;
        _customerResolver = customerResolver;
        _assignmentResolver = assignmentResolver;
        _entityLinker = entityLinker;
        _notifier = notifier;
        _auditScope = auditScope;
        _auditService = auditService;
        _currentUser = currentUser;
        _clock = clock;
        _mapper = mapper;
    }

    public async Task<Result<WorkflowInstanceDto>> Handle(
        StartWorkflowCommand request,
        CancellationToken cancellationToken)
    {
        WorkflowDefinition? definition;
        if (request.DefinitionId.HasValue)
        {
            definition = await _definitions.GetByIdWithGraphAsync(
                request.DefinitionId.Value,
                cancellationToken);
        }
        else if (string.IsNullOrWhiteSpace(request.DefinitionCode))
        {
            definition = await _definitions.GetPublishedByEntityTypeAsync(
                request.EntityType,
                cancellationToken);
        }
        else
        {
            definition = await _definitions.GetByCodeAsync(
                request.DefinitionCode.Trim(),
                cancellationToken);
        }

        if (definition is null)
        {
            return Result.Failure<WorkflowInstanceDto>(
                "تعریف گردش کار یافت نشد.",
                "WORKFLOW_DEFINITION_NOT_FOUND");
        }

        if (definition.Status != WorkflowDefinitionStatus.Published)
        {
            return Result.Failure<WorkflowInstanceDto>(
                "فقط گردش کار منتشرشده قابل شروع است.",
                "WORKFLOW_DEFINITION_NOT_PUBLISHED");
        }

        if (!string.IsNullOrWhiteSpace(request.IdempotencyKey))
        {
            var existing = await _instances.GetByIdempotencyKeyAsync(
                request.IdempotencyKey,
                cancellationToken);
            if (existing is not null)
            {
                return Result.Success(_mapper.ToInstance(existing, definition));
            }
        }

        var active = await _instances.GetActiveByEntityAsync(
            request.EntityType,
            request.EntityId,
            cancellationToken);
        if (active is not null)
        {
            return Result.Failure<WorkflowInstanceDto>(
                "برای این موجودیت یک گردش کار فعال وجود دارد.",
                "WORKFLOW_ACTIVE_INSTANCE_EXISTS");
        }

        var startState = definition.States.SingleOrDefault(
            state => state.StateType == WorkflowStateType.Start);
        if (startState is null)
        {
            return Result.Failure<WorkflowInstanceDto>(
                "گردش کار وضعیت شروع ندارد.",
                "WORKFLOW_START_REQUIRED");
        }

        var now = _clock.UtcNow;
        var actor = await _actorAccessor.GetAsync(cancellationToken);
        var owner = await _customerResolver.GetCustomerOwnerUserIdAsync(
            request.EntityType,
            request.EntityId,
            cancellationToken);
        var customerUser = await _customerResolver.GetCustomerPortalUserIdAsync(
            request.EntityType,
            request.EntityId,
            cancellationToken);
        var assignment = _assignmentResolver.Resolve(startState, owner, customerUser);
        if (definition.Code == BandwidthIntake.Code && request.InitialPayload is not null)
        {
            using var payload = JsonDocument.Parse(request.InitialPayload);
            if (payload.RootElement.TryGetProperty("fromPortal", out var portal) && !portal.GetBoolean())
                assignment = new WorkflowAssignment(actor.UserId, null, null);
        }

        var instance = new WorkflowInstance(
            definition,
            request.EntityType,
            request.EntityId,
            startState.Id,
            _currentUser.UserId ?? actor.UserId,
            now,
            correlationId: Guid.NewGuid().ToString("N"),
            idempotencyKey: request.IdempotencyKey);

        instance.AddStartHistory(
            startState.Id,
            _currentUser.UserId ?? actor.UserId,
            now,
            "شروع گردش کار",
            request.InitialPayload);

        var task = new WorkflowTask(
            instance.Id,
            startState.Id,
            assignment.UserId,
            assignment.RoleId,
            assignment.DepartmentId,
            dueDate: null,
            createdBy: _currentUser.UserId ?? actor.UserId,
            createdAtUtc: now);
        instance.AddTask(task);

        try
        {
            _instances.Add(instance);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            await _entityLinker.LinkAsync(instance, cancellationToken);
            _auditScope.EntityId = instance.Id.ToString();

            await _auditService.RecordAsync(
                new AuditEntry(
                    "WorkflowInstance",
                    instance.Id.ToString(),
                    AuditAction.Create,
                    null,
                    new
                    {
                        FromState = (string?)null,
                        ToState = startState.Code,
                        ActionBy = _currentUser.UserId,
                        ActionAt = now,
                        Reason = "شروع گردش کار",
                        EntityType = instance.EntityType.ToString(),
                        EntityId = instance.EntityId,
                    },
                    _currentUser.UserId),
                cancellationToken);

            await TryNotifyAsync(
                () => _notifier.NotifyTaskAssignedAsync(task, instance, definition, cancellationToken),
                cancellationToken);

            var saved = await _instances.GetByIdWithGraphAsync(instance.Id, cancellationToken);
            return Result.Success(_mapper.ToInstance(saved ?? instance, definition));
        }
        catch (DomainException exception)
        {
            return Result.Failure<WorkflowInstanceDto>(exception.Message, exception.Code);
        }
    }

    private static async Task TryNotifyAsync(
        Func<Task> notification,
        CancellationToken cancellationToken)
    {
        try
        {
            await notification();
        }
        catch
        {
            // Notification delivery must not roll back a saved workflow transition.
        }
    }
}

public sealed class ExecuteWorkflowTransitionCommandHandler
    : IRequestHandler<ExecuteWorkflowTransitionCommand, Result<WorkflowInstanceDto>>
{
    private readonly IWorkflowDefinitionRepository _definitions;
    private readonly IWorkflowInstanceRepository _instances;
    private readonly IWorkflowUnitOfWork _unitOfWork;
    private readonly IWorkflowActorAccessor _actorAccessor;
    private readonly IWorkflowEntityCustomerResolver _customerResolver;
    private readonly WorkflowAssignmentResolver _assignmentResolver;
    private readonly IEnumerable<IWorkflowGuard> _guards;
    private readonly IEnumerable<IWorkflowTransitionHook> _hooks;
    private readonly IWorkflowNotifier _notifier;
    private readonly IAuditScope _auditScope;
    private readonly IAuditService _auditService;
    private readonly FoundationCurrentUser _currentUser;
    private readonly IDateTimeProvider _clock;
    private readonly WorkflowDtoMapper _mapper;
    private readonly IOrderRepository? _intakeOrders;

    public ExecuteWorkflowTransitionCommandHandler(
        IWorkflowDefinitionRepository definitions,
        IWorkflowInstanceRepository instances,
        IWorkflowUnitOfWork unitOfWork,
        IWorkflowActorAccessor actorAccessor,
        IWorkflowEntityCustomerResolver customerResolver,
        WorkflowAssignmentResolver assignmentResolver,
        IEnumerable<IWorkflowGuard> guards,
        IEnumerable<IWorkflowTransitionHook> hooks,
        IWorkflowNotifier notifier,
        IAuditScope auditScope,
        IAuditService auditService,
        FoundationCurrentUser currentUser,
        IDateTimeProvider clock,
        WorkflowDtoMapper mapper,
        IOrderRepository? intakeOrders = null)
    {
        _definitions = definitions;
        _instances = instances;
        _unitOfWork = unitOfWork;
        _actorAccessor = actorAccessor;
        _customerResolver = customerResolver;
        _assignmentResolver = assignmentResolver;
        _guards = guards;
        _hooks = hooks;
        _notifier = notifier;
        _auditScope = auditScope;
        _auditService = auditService;
        _currentUser = currentUser;
        _clock = clock;
        _mapper = mapper;
        _intakeOrders = intakeOrders;
    }

    public async Task<Result<WorkflowInstanceDto>> Handle(
        ExecuteWorkflowTransitionCommand request,
        CancellationToken cancellationToken)
    {
        var instance = await _instances.GetByIdWithGraphAsync(request.InstanceId, cancellationToken);
        if (instance is null)
        {
            return Result.Failure<WorkflowInstanceDto>(
                "نمونه گردش کار یافت نشد.",
                "WORKFLOW_INSTANCE_NOT_FOUND");
        }

        var definition = await _definitions.GetByIdWithGraphAsync(
            instance.DefinitionId,
            cancellationToken);
        if (definition is null)
        {
            return Result.Failure<WorkflowInstanceDto>(
                "تعریف گردش کار یافت نشد.",
                "WORKFLOW_DEFINITION_NOT_FOUND");
        }

        if (!string.IsNullOrWhiteSpace(request.IdempotencyKey)
            && instance.History.Any(history =>
                string.Equals(history.IdempotencyKey, request.IdempotencyKey, StringComparison.Ordinal)))
        {
            return Result.Success(_mapper.ToInstance(instance, definition));
        }

        if (instance.Status != WorkflowInstanceStatus.Running)
        {
            return Result.Failure<WorkflowInstanceDto>(
                "گردش کار در وضعیت اجرا نیست.",
                "WORKFLOW_INSTANCE_NOT_RUNNING");
        }

        var transition = definition.Transitions.SingleOrDefault(item => item.Id == request.TransitionId);
        if (transition is null)
        {
            return Result.Failure<WorkflowInstanceDto>(
                "گذار یافت نشد.",
                "WORKFLOW_TRANSITION_NOT_FOUND");
        }

        var currentState = definition.FindState(instance.CurrentStateId);
        if (transition.FromStateId != currentState.Id)
        {
            return Result.Failure<WorkflowInstanceDto>(
                "گذار انتخاب‌شده از وضعیت فعلی قابل اجرا نیست.",
                "WORKFLOW_TRANSITION_NOT_ALLOWED");
        }

        if (transition.RequiresComment && string.IsNullOrWhiteSpace(request.Comment))
        {
            return Result.Failure<WorkflowInstanceDto>(
                "درج توضیح برای این گذار الزامی است.",
                "WORKFLOW_COMMENT_REQUIRED");
        }

        var targetState = definition.FindState(transition.ToStateId);
        var actor = await _actorAccessor.GetAsync(cancellationToken);
        if (definition.Code == BandwidthIntake.Code)
        {
            var intakeOrder = _intakeOrders is null ? null : await _intakeOrders.GetByIdWithDetailsAsync(instance.EntityId, cancellationToken);
            var validated = BandwidthIntake.ValidateTransition(instance, definition, transition,
                intakeOrder?.Items.FirstOrDefault()?.AttributesJson ?? "{}", actor, request.PayloadJson);
            if (validated.IsFailure) return Result.Failure<WorkflowInstanceDto>(validated.Errors, validated.Message);
            if (currentState.FormCode is not null && !request.ActionValidated)
                return Result.Failure<WorkflowInstanceDto>("ابتدا فرم این مرحله را از کارتابل ثبت کنید.", "WORKFLOW_FORM_ACTION_REQUIRED");
            if (currentState.StateType == WorkflowStateType.Start && actor.UserId.HasValue)
                intakeOrder?.AssignSalesExpert(actor.UserId.Value, _clock.UtcNow);
        }

        if (!await GuardAsync(instance, transition, currentState, targetState, actor, request))
        {
            return Result.Failure<WorkflowInstanceDto>(
                "شرایط اجرای این گذار فراهم نیست.",
                "WORKFLOW_GUARD_FAILED");
        }

        var owner = await _customerResolver.GetCustomerOwnerUserIdAsync(
            instance.EntityType,
            instance.EntityId,
            cancellationToken);
        var customerUser = await _customerResolver.GetCustomerPortalUserIdAsync(
            instance.EntityType,
            instance.EntityId,
            cancellationToken);
        var assignment = _assignmentResolver.Resolve(targetState, owner, customerUser, BandwidthIntake.SalesOwner(instance, definition));
        // Explicit expert selection belongs only to the report assignment, never to the manager review or sales return.
        var explicitAssignee = definition.Code == BandwidthIntake.Code
            ? currentState.Code == "FEASIBILITY_TRIAGE" ? ReadExplicitAssignee(request.PayloadJson) ?? actor.UserId : null
            : ReadExplicitAssignee(request.PayloadJson);
        if (definition.Code == BandwidthIntake.Code && currentState.Code == "FEASIBILITY_REVIEW" && targetState.Code == "FEASIBILITY_REPORT")
            explicitAssignee = instance.Tasks.Where(task => task.StateId == targetState.Id).OrderByDescending(task => task.CreatedAt).FirstOrDefault()?.AssignedUserId;
        if (definition.Code == BandwidthIntake.Code && currentState.StateType == WorkflowStateType.Start && targetState.AssigneeType == WorkflowAssigneeType.RequestSalesOwner)
            assignment = new WorkflowAssignment(actor.UserId, null, null);
        if (explicitAssignee.HasValue)
        {
            assignment = new WorkflowAssignment(explicitAssignee.Value, null, null);
        }

        var now = _clock.UtcNow;
        // Only one task may be active for a state. If the state has several assigned
        // tasks (for example after a manual reassignment), close the older ones.
        var staleTasks = instance.Tasks
            .Where(task => task.StateId == currentState.Id
                           && task.Status is WorkflowTaskStatus.Pending or WorkflowTaskStatus.InProgress)
            .OrderByDescending(task => task.CreatedAt)
            .ToArray();
        var currentTask = staleTasks.FirstOrDefault();
        foreach (var staleTask in staleTasks.Skip(1))
        {
            staleTask.Complete(_currentUser.UserId ?? actor.UserId, now);
        }

        WorkflowTask? nextTask = null;
        if (targetState.StateType != WorkflowStateType.End && !targetState.IsFinal)
        {
            nextTask = new WorkflowTask(
                instance.Id,
                targetState.Id,
                assignment.UserId,
                assignment.RoleId,
                assignment.DepartmentId,
                dueDate: null,
                createdBy: _currentUser.UserId ?? actor.UserId,
                createdAtUtc: now);
        }

        try
        {
            foreach (var hook in _hooks)
            {
                await hook.OnExitAsync(instance, currentState, transition, cancellationToken);
            }

            instance.ExecuteTransition(
                transition,
                targetState,
                _currentUser.UserId ?? actor.UserId,
                now,
                request.Comment,
                request.PayloadJson,
                request.IdempotencyKey,
                currentTask,
                nextTask);

            foreach (var hook in _hooks)
            {
                await hook.OnEnterAsync(instance, targetState, transition, cancellationToken);
            }

            await _unitOfWork.SaveChangesAsync(cancellationToken);
            _auditScope.EntityId = instance.Id.ToString();

            await _auditService.RecordAsync(
                new AuditEntry(
                    "WorkflowInstance",
                    instance.Id.ToString(),
                    AuditAction.Update,
                    new
                    {
                        FromState = currentState.Code,
                        ToState = (string?)null,
                        Status = WorkflowInstanceStatus.Running.ToString(),
                    },
                    new
                    {
                        FromState = currentState.Code,
                        ToState = targetState.Code,
                        Transition = transition.Name,
                        TransitionId = transition.Id,
                        ActionBy = _currentUser.UserId ?? actor.UserId,
                        ActionAt = now,
                        Reason = request.Comment,
                        Comment = request.Comment,
                        Payload = request.PayloadJson,
                    },
                    _currentUser.UserId ?? actor.UserId),
                cancellationToken);

            if (nextTask is not null)
            {
                await TryNotifyAsync(
                    () => _notifier.NotifyTaskAssignedAsync(nextTask, instance, definition, cancellationToken),
                    cancellationToken);
            }
            else
            {
                await TryNotifyAsync(
                    () => _notifier.NotifyStateEnteredAsync(instance, targetState, cancellationToken),
                    cancellationToken);
            }

            var saved = await _instances.GetByIdWithGraphAsync(instance.Id, cancellationToken);
            return Result.Success(_mapper.ToInstance(saved ?? instance, definition));
        }
        catch (DomainException exception)
        {
            return Result.Failure<WorkflowInstanceDto>(exception.Message, exception.Code);
        }
    }

    private static Guid? ReadExplicitAssignee(string? payloadJson)
    {
        if (string.IsNullOrWhiteSpace(payloadJson))
        {
            return null;
        }

        try
        {
            using var document = JsonDocument.Parse(payloadJson);
            foreach (var key in new[] { "expert_user_id", "expertUserId", "assignedUserId" })
            {
                if (document.RootElement.TryGetProperty(key, out var value)
                    && value.ValueKind == JsonValueKind.String
                    && Guid.TryParse(value.GetString(), out var userId))
                {
                    return userId;
                }
            }
        }
        catch (JsonException)
        {
            return null;
        }

        return null;
    }

    private async Task<bool> GuardAsync(
        WorkflowInstance instance,
        WorkflowTransition transition,
        WorkflowState currentState,
        WorkflowState targetState,
        WorkflowActor actor,
        ExecuteWorkflowTransitionCommand request)
    {
        if (transition.GuardType == WorkflowGuardType.None)
        {
            return true;
        }

        if (transition.GuardType == WorkflowGuardType.PermissionOnly)
        {
            return string.IsNullOrWhiteSpace(transition.RequiredPermission)
                   || actor.Permissions.Contains(Permissions.Wildcard, StringComparer.Ordinal)
                   || actor.Permissions.Contains(transition.RequiredPermission, StringComparer.Ordinal);
        }

        var guard = _guards.FirstOrDefault(item =>
            string.Equals(item.Key, transition.GuardPluginKey, StringComparison.OrdinalIgnoreCase));
        return guard is not null
               && await guard.CanExecuteAsync(
                   new WorkflowGuardContext(
                       instance,
                       transition,
                       currentState,
                       targetState,
                       actor,
                       request.Comment,
                       request.PayloadJson));
    }

    private static async Task TryNotifyAsync(
        Func<Task> notification,
        CancellationToken cancellationToken)
    {
        try
        {
            await notification();
        }
        catch
        {
            // Notification delivery must not roll back a saved workflow transition.
        }
    }
}

public sealed class CancelWorkflowInstanceCommandHandler
    : IRequestHandler<CancelWorkflowInstanceCommand, Result<WorkflowInstanceDto>>
{
    private readonly IWorkflowDefinitionRepository _definitions;
    private readonly IWorkflowInstanceRepository _instances;
    private readonly IWorkflowUnitOfWork _unitOfWork;
    private readonly IAuditScope _auditScope;
    private readonly IAuditService _auditService;
    private readonly FoundationCurrentUser _currentUser;
    private readonly IDateTimeProvider _clock;
    private readonly WorkflowDtoMapper _mapper;

    public CancelWorkflowInstanceCommandHandler(
        IWorkflowDefinitionRepository definitions,
        IWorkflowInstanceRepository instances,
        IWorkflowUnitOfWork unitOfWork,
        IAuditScope auditScope,
        IAuditService auditService,
        FoundationCurrentUser currentUser,
        IDateTimeProvider clock,
        WorkflowDtoMapper mapper)
    {
        _definitions = definitions;
        _instances = instances;
        _unitOfWork = unitOfWork;
        _auditScope = auditScope;
        _auditService = auditService;
        _currentUser = currentUser;
        _clock = clock;
        _mapper = mapper;
    }

    public async Task<Result<WorkflowInstanceDto>> Handle(
        CancelWorkflowInstanceCommand request,
        CancellationToken cancellationToken)
    {
        var instance = await _instances.GetByIdWithGraphAsync(request.InstanceId, cancellationToken);
        if (instance is null)
        {
            return Result.Failure<WorkflowInstanceDto>(
                "نمونه گردش کار یافت نشد.",
                "WORKFLOW_INSTANCE_NOT_FOUND");
        }

        var definition = await _definitions.GetByIdWithGraphAsync(instance.DefinitionId, cancellationToken);
        if (definition is null)
        {
            return Result.Failure<WorkflowInstanceDto>(
                "تعریف گردش کار یافت نشد.",
                "WORKFLOW_DEFINITION_NOT_FOUND");
        }

        try
        {
            instance.Cancel(_currentUser.UserId, _clock.UtcNow, request.Reason);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            _auditScope.EntityId = instance.Id.ToString();

            await _auditService.RecordAsync(
                new AuditEntry(
                    "WorkflowInstance",
                    instance.Id.ToString(),
                    AuditAction.Update,
                    new { Status = WorkflowInstanceStatus.Running.ToString() },
                    new
                    {
                        Status = WorkflowInstanceStatus.Cancelled.ToString(),
                        Reason = request.Reason,
                        ActionBy = _currentUser.UserId,
                    },
                    _currentUser.UserId),
                cancellationToken);

            return Result.Success(_mapper.ToInstance(instance, definition));
        }
        catch (DomainException exception)
        {
            return Result.Failure<WorkflowInstanceDto>(exception.Message, exception.Code);
        }
    }
}

public sealed class ReassignWorkflowTaskCommandHandler
    : IRequestHandler<ReassignWorkflowTaskCommand, Result<WorkflowTaskDto>>
{
    private readonly IWorkflowDefinitionRepository _definitions;
    private readonly IWorkflowInstanceRepository _instances;
    private readonly IWorkflowUnitOfWork _unitOfWork;
    private readonly IAuditScope _auditScope;
    private readonly IAuditService _auditService;
    private readonly FoundationCurrentUser _currentUser;
    private readonly IDateTimeProvider _clock;
    private readonly WorkflowDtoMapper _mapper;

    public ReassignWorkflowTaskCommandHandler(
        IWorkflowDefinitionRepository definitions,
        IWorkflowInstanceRepository instances,
        IWorkflowUnitOfWork unitOfWork,
        IAuditScope auditScope,
        IAuditService auditService,
        FoundationCurrentUser currentUser,
        IDateTimeProvider clock,
        WorkflowDtoMapper mapper)
    {
        _definitions = definitions;
        _instances = instances;
        _unitOfWork = unitOfWork;
        _auditScope = auditScope;
        _auditService = auditService;
        _currentUser = currentUser;
        _clock = clock;
        _mapper = mapper;
    }

    public async Task<Result<WorkflowTaskDto>> Handle(
        ReassignWorkflowTaskCommand request,
        CancellationToken cancellationToken)
    {
        var task = await _instances.GetTaskWithGraphAsync(request.TaskId, cancellationToken);
        if (task is null)
        {
            return Result.Failure<WorkflowTaskDto>(
                "تسک یافت نشد.",
                "WORKFLOW_TASK_NOT_FOUND");
        }

        var instance = await _instances.GetByIdWithGraphAsync(task.InstanceId, cancellationToken);
        if (instance is null)
        {
            return Result.Failure<WorkflowTaskDto>(
                "نمونه گردش کار یافت نشد.",
                "WORKFLOW_INSTANCE_NOT_FOUND");
        }

        var definition = await _definitions.GetByIdWithGraphAsync(instance.DefinitionId, cancellationToken);
        if (definition is null)
        {
            return Result.Failure<WorkflowTaskDto>(
                "تعریف گردش کار یافت نشد.",
                "WORKFLOW_DEFINITION_NOT_FOUND");
        }

        try
        {
            task.Reassign(
                request.UserId,
                request.RoleId,
                request.DepartmentId,
                _currentUser.UserId,
                _clock.UtcNow);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            _auditScope.EntityId = task.Id.ToString();

            await _auditService.RecordAsync(
                new AuditEntry(
                    "WorkflowTask",
                    task.Id.ToString(),
                    AuditAction.Update,
                    null,
                    new
                    {
                        AssignedUserId = request.UserId,
                        AssignedRoleId = request.RoleId,
                        AssignedDepartmentId = request.DepartmentId,
                        ActionBy = _currentUser.UserId,
                    },
                    _currentUser.UserId),
                cancellationToken);

            return Result.Success(_mapper.ToTask(task, instance, definition));
        }
        catch (DomainException exception)
        {
            return Result.Failure<WorkflowTaskDto>(exception.Message, exception.Code);
        }
    }
}
