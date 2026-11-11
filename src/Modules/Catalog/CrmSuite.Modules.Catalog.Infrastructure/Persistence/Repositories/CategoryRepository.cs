using CrmSuite.BuildingBlocks.Infrastructure.Persistence;
using CrmSuite.Modules.Catalog.Application.Abstractions;
using CrmSuite.Modules.Catalog.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace CrmSuite.Modules.Catalog.Infrastructure.Persistence.Repositories;

public sealed class CategoryRepository : ICategoryRepository
{
    private readonly AppDbContext _dbContext;

    public CategoryRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<ServiceCategory>> GetAllAsync(CancellationToken cancellationToken) =>
        await _dbContext.Set<ServiceCategory>()
            .AsNoTracking()
            .OrderBy(category => category.Name)
            .ToListAsync(cancellationToken);

    public Task<ServiceCategory?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        _dbContext.Set<ServiceCategory>()
            .SingleOrDefaultAsync(category => category.Id == id, cancellationToken);

    public Task<bool> NameExistsAsync(
        string name,
        Guid? parentId,
        Guid? excludeId,
        CancellationToken cancellationToken)
    {
        var query = _dbContext.Set<ServiceCategory>()
            .Where(category => category.ParentId == parentId && category.Name == name.Trim());

        if (excludeId.HasValue)
        {
            query = query.Where(category => category.Id != excludeId.Value);
        }

        return query.AnyAsync(cancellationToken);
    }

    public Task<bool> HasChildrenAsync(Guid id, CancellationToken cancellationToken) =>
        _dbContext.Set<ServiceCategory>()
            .AnyAsync(category => category.ParentId == id, cancellationToken);

    public Task<bool> HasServicesAsync(Guid id, CancellationToken cancellationToken) =>
        _dbContext.Set<Service>().AnyAsync(service => service.CategoryId == id, cancellationToken);

    public async Task<bool> IsDescendantAsync(
        Guid id,
        Guid potentialParentId,
        CancellationToken cancellationToken)
    {
        var current = potentialParentId;
        var visited = new HashSet<Guid>();

        while (visited.Add(current))
        {
            if (current == id)
            {
                return true;
            }

            var parentId = await _dbContext.Set<ServiceCategory>()
                .Where(category => category.Id == current)
                .Select(category => category.ParentId)
                .FirstOrDefaultAsync(cancellationToken);

            if (!parentId.HasValue)
            {
                return false;
            }

            current = parentId.Value;
        }

        return false;
    }

    public void Add(ServiceCategory category) => _dbContext.Set<ServiceCategory>().Add(category);

    public void Remove(ServiceCategory category) => _dbContext.Set<ServiceCategory>().Remove(category);
}
