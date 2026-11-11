using CrmSuite.BuildingBlocks.Infrastructure.Persistence;
using CrmSuite.Modules.Workflow.Application.Abstractions;
using CrmSuite.Modules.Workflow.Domain.Entities;
using CrmSuite.Modules.Workflow.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace CrmSuite.Modules.Workflow.Infrastructure.Persistence.Repositories;

public sealed class WorkflowDefinitionRepository : IWorkflowDefinitionRepository
{
    private readonly AppDbContext _dbContext;

    public WorkflowDefinitionRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<WorkflowDefinition?> GetByIdWithGraphAsync(
        Guid id,
        CancellationToken cancellationToken) =>
        WithGraph()
            .SingleOrDefaultAsync(definition => definition.Id == id, cancellationToken);

    public Task<WorkflowDefinition?> GetByCodeAsync(
        string code,
        CancellationToken cancellationToken)
    {
        var normalized = code.Trim().ToUpperInvariant();
        return WithGraph()
            .SingleOrDefaultAsync(definition => definition.Code == normalized, cancellationToken);
    }

    public Task<WorkflowDefinition?> GetPublishedByEntityTypeAsync(
        WorkflowEntityType entityType,
        CancellationToken cancellationToken) =>
        WithGraph()
            .Where(definition =>
                definition.EntityType == entityType
                && definition.Status == WorkflowDefinitionStatus.Published)
            .OrderByDescending(definition => definition.PublishedAt)
            .ThenByDescending(definition => definition.Version)
            .FirstOrDefaultAsync(cancellationToken);

    public async Task<(IReadOnlyList<WorkflowDefinition> Items, int TotalCount)> GetPagedAsync(
        WorkflowDefinitionFilter filter,
        CancellationToken cancellationToken)
    {
        var query = WithGraph().AsNoTracking();

        if (filter.Status.HasValue)
        {
            query = query.Where(definition => definition.Status == filter.Status.Value);
        }

        if (filter.EntityType.HasValue)
        {
            query = query.Where(definition => definition.EntityType == filter.EntityType.Value);
        }

        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            var term = filter.Search.Trim();
            query = query.Where(definition =>
                definition.Code.Contains(term)
                || definition.Name.Contains(term));
        }

        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(definition => definition.CreatedAt)
            .Skip((filter.Page - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .ToListAsync(cancellationToken);

        return (items, total);
    }

    public Task<bool> CodeExistsAsync(
        string code,
        Guid? excludeId,
        CancellationToken cancellationToken)
    {
        var normalized = code.Trim().ToUpperInvariant();
        var query = _dbContext.Set<WorkflowDefinition>()
            .Where(definition => definition.Code == normalized);

        if (excludeId.HasValue)
        {
            query = query.Where(definition => definition.Id != excludeId.Value);
        }

        return query.AnyAsync(cancellationToken);
    }

    public void Add(WorkflowDefinition definition) =>
        _dbContext.Set<WorkflowDefinition>().Add(definition);

    private IQueryable<WorkflowDefinition> WithGraph() =>
        _dbContext.Set<WorkflowDefinition>()
            .Include(definition => definition.States)
            .Include(definition => definition.Transitions);
}
