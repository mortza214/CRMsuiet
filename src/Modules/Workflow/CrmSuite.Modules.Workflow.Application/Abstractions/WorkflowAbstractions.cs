using CrmSuite.Modules.Workflow.Application.Dtos;
using CrmSuite.Modules.Workflow.Domain.Entities;
using CrmSuite.Modules.Workflow.Domain.Enums;

namespace CrmSuite.Modules.Workflow.Application.Abstractions;

public sealed record WorkflowDefinitionFilter(
    WorkflowDefinitionStatus? Status,
    WorkflowEntityType? EntityType,
    string? Search,
    int Page,
    int PageSize);

public sealed record WorkflowTaskFilter(
    WorkflowTaskStatus? Status,
    Guid? DepartmentId,
    int Page,
    int PageSize);

public sealed record WorkflowActor(
    Guid? UserId,
    IReadOnlyCollection<Guid> RoleIds,
    IReadOnlyCollection<Guid> DepartmentIds,
    IReadOnlyCollection<string> Permissions);

public sealed record WorkflowAssignment(
    Guid? UserId,
    Guid? RoleId,
    Guid? DepartmentId);

public sealed record WorkflowGuardContext(
    WorkflowInstance Instance,
    WorkflowTransition Transition,
    WorkflowState CurrentState,
    WorkflowState TargetState,
    WorkflowActor Actor,
    string? Comment,
    string? PayloadJson);

public interface IWorkflowDefinitionRepository
{
    Task<WorkflowDefinition?> GetByIdWithGraphAsync(Guid id, CancellationToken cancellationToken);

    Task<WorkflowDefinition?> GetByCodeAsync(string code, CancellationToken cancellationToken);

    Task<WorkflowDefinition?> GetPublishedByEntityTypeAsync(
        WorkflowEntityType entityType,
        CancellationToken cancellationToken);

    Task<(IReadOnlyList<WorkflowDefinition> Items, int TotalCount)> GetPagedAsync(
        WorkflowDefinitionFilter filter,
        CancellationToken cancellationToken);

    Task<bool> CodeExistsAsync(string code, Guid? excludeId, CancellationToken cancellationToken);

    void Add(WorkflowDefinition definition);
}

public interface IWorkflowInstanceRepository
{
    Task<WorkflowInstance?> GetByIdWithGraphAsync(Guid id, CancellationToken cancellationToken);

    Task<WorkflowInstance?> GetByIdempotencyKeyAsync(
        string idempotencyKey,
        CancellationToken cancellationToken);

    Task<WorkflowInstance?> GetActiveByEntityAsync(
        WorkflowEntityType entityType,
        Guid entityId,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<WorkflowInstance>> GetByEntityAsync(
        WorkflowEntityType entityType,
        Guid entityId,
        CancellationToken cancellationToken);

    Task<WorkflowTask?> GetTaskWithGraphAsync(Guid taskId, CancellationToken cancellationToken);

    Task<(IReadOnlyList<WorkflowTask> Items, int TotalCount)> GetMyTasksAsync(
        WorkflowActor actor,
        WorkflowTaskFilter filter,
        CancellationToken cancellationToken);

    void Add(WorkflowInstance instance);
}

public interface IWorkflowUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}

public interface IWorkflowActorAccessor
{
    Task<WorkflowActor> GetAsync(CancellationToken cancellationToken);
}

public interface IWorkflowEntityCustomerResolver
{
    Task<Guid?> GetCustomerOwnerUserIdAsync(
        WorkflowEntityType entityType,
        Guid entityId,
        CancellationToken cancellationToken);

    Task<Guid?> GetCustomerPortalUserIdAsync(
        WorkflowEntityType entityType,
        Guid entityId,
        CancellationToken cancellationToken);
}

public interface IWorkflowEntityLinker
{
    Task LinkAsync(WorkflowInstance instance, CancellationToken cancellationToken);
}

public interface IWorkflowGuard
{
    string Key { get; }

    Task<bool> CanExecuteAsync(WorkflowGuardContext context);
}

public interface IWorkflowTransitionHook
{
    Task OnExitAsync(
        WorkflowInstance instance,
        WorkflowState fromState,
        WorkflowTransition transition,
        CancellationToken cancellationToken);

    Task OnEnterAsync(
        WorkflowInstance instance,
        WorkflowState toState,
        WorkflowTransition transition,
        CancellationToken cancellationToken);
}

public interface IWorkflowNotifier
{
    Task NotifyTaskAssignedAsync(
        WorkflowTask task,
        WorkflowInstance instance,
        WorkflowDefinition definition,
        CancellationToken cancellationToken);

    Task NotifyStateEnteredAsync(
        WorkflowInstance instance,
        WorkflowState state,
        CancellationToken cancellationToken);
}

public interface IWorkflowStarter
{
    Task<WorkflowInstanceDto?> TryStartAsync(
        WorkflowEntityType entityType,
        Guid entityId,
        string? definitionCode,
        string? initialPayload,
        string? idempotencyKey,
        CancellationToken cancellationToken);
}
