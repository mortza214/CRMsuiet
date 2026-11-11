using CrmSuite.BuildingBlocks.Infrastructure.Persistence;
using CrmSuite.Modules.Workflow.Application.Abstractions;
using CrmSuite.Modules.Workflow.Domain.Entities;
using CrmSuite.Modules.Workflow.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace CrmSuite.Modules.Workflow.Infrastructure.Persistence.Repositories;

public sealed class WorkflowInstanceRepository : IWorkflowInstanceRepository
{
    private readonly AppDbContext _dbContext;

    public WorkflowInstanceRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<WorkflowInstance?> GetByIdWithGraphAsync(
        Guid id,
        CancellationToken cancellationToken) =>
        WithGraph()
            .SingleOrDefaultAsync(instance => instance.Id == id, cancellationToken);

    public Task<WorkflowInstance?> GetByIdempotencyKeyAsync(
        string idempotencyKey,
        CancellationToken cancellationToken) =>
        WithGraph()
            .SingleOrDefaultAsync(
                instance => instance.IdempotencyKey == idempotencyKey,
                cancellationToken);

    public Task<WorkflowInstance?> GetActiveByEntityAsync(
        WorkflowEntityType entityType,
        Guid entityId,
        CancellationToken cancellationToken) =>
        WithGraph()
            .SingleOrDefaultAsync(
                instance => instance.EntityType == entityType
                            && instance.EntityId == entityId
                            && instance.Status == WorkflowInstanceStatus.Running,
                cancellationToken);

    public async Task<IReadOnlyList<WorkflowInstance>> GetByEntityAsync(
        WorkflowEntityType entityType,
        Guid entityId,
        CancellationToken cancellationToken) =>
        await WithGraph()
            .Where(instance => instance.EntityType == entityType && instance.EntityId == entityId)
            .OrderByDescending(instance => instance.CreatedAt)
            .ToListAsync(cancellationToken);

    public Task<WorkflowTask?> GetTaskWithGraphAsync(
        Guid taskId,
        CancellationToken cancellationToken) =>
        _dbContext.Set<WorkflowTask>()
            .SingleOrDefaultAsync(task => task.Id == taskId, cancellationToken);

    public async Task<(IReadOnlyList<WorkflowTask> Items, int TotalCount)> GetMyTasksAsync(
        WorkflowActor actor,
        WorkflowTaskFilter filter,
        CancellationToken cancellationToken)
    {
        var query = _dbContext.Set<WorkflowTask>()
            .AsNoTracking()
            .Where(task =>
                task.Status == WorkflowTaskStatus.Pending
                || task.Status == WorkflowTaskStatus.InProgress);

        if (filter.Status.HasValue)
        {
            query = query.Where(task => task.Status == filter.Status.Value);
        }

        if (filter.DepartmentId.HasValue)
        {
            query = query.Where(task => task.AssignedDepartmentId == filter.DepartmentId.Value);
        }

        var roleIds = actor.RoleIds.ToArray();
        var departmentIds = actor.DepartmentIds.ToArray();
        query = query.Where(task =>
            (actor.UserId.HasValue && task.AssignedUserId == actor.UserId.Value)
            || (task.AssignedRoleId.HasValue && roleIds.Contains(task.AssignedRoleId.Value))
            || (task.AssignedDepartmentId.HasValue
                && departmentIds.Contains(task.AssignedDepartmentId.Value)));

        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderBy(task => task.CreatedAt)
            .Skip((filter.Page - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .ToListAsync(cancellationToken);

        return (items, total);
    }

    public void Add(WorkflowInstance instance) =>
        _dbContext.Set<WorkflowInstance>().Add(instance);

    private IQueryable<WorkflowInstance> WithGraph() =>
        _dbContext.Set<WorkflowInstance>()
            .Include(instance => instance.Tasks)
            .Include(instance => instance.History);
}
