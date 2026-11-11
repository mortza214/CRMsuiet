using CrmSuite.BuildingBlocks.Infrastructure.Persistence;
using CrmSuite.Modules.Identity.Application.Abstractions;
using CrmSuite.Modules.Identity.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace CrmSuite.Modules.Identity.Infrastructure.Persistence.Repositories;

public sealed class DepartmentRepository : IDepartmentRepository
{
    private readonly AppDbContext _dbContext;

    public DepartmentRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<Department?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        _dbContext.Set<Department>().SingleOrDefaultAsync(department => department.Id == id, cancellationToken);

    public async Task<IReadOnlyList<Department>> GetAllAsync(CancellationToken cancellationToken) =>
        await _dbContext.Set<Department>()
            .AsNoTracking()
            .OrderBy(department => department.Name)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<Department>> GetByIdsAsync(
        IReadOnlyCollection<Guid> ids,
        CancellationToken cancellationToken)
    {
        if (ids.Count == 0)
        {
            return [];
        }

        return await _dbContext.Set<Department>()
            .Where(department => ids.Contains(department.Id))
            .ToListAsync(cancellationToken);
    }

    public Task<bool> NameExistsAsync(
        string name,
        Guid? parentId,
        Guid? excludeId,
        CancellationToken cancellationToken)
    {
        var query = _dbContext.Set<Department>()
            .Where(department => department.ParentId == parentId && department.Name == name.Trim());

        if (excludeId.HasValue)
        {
            query = query.Where(department => department.Id != excludeId.Value);
        }

        return query.AnyAsync(cancellationToken);
    }

    public Task<bool> HasChildrenAsync(Guid id, CancellationToken cancellationToken) =>
        _dbContext.Set<Department>().AnyAsync(department => department.ParentId == id, cancellationToken);

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

            var parentId = await _dbContext.Set<Department>()
                .Where(department => department.Id == current)
                .Select(department => department.ParentId)
                .FirstOrDefaultAsync(cancellationToken);

            if (!parentId.HasValue)
            {
                return false;
            }

            current = parentId.Value;
        }

        return false;
    }

    public Task<int> CountUsersAsync(Guid id, CancellationToken cancellationToken) =>
        _dbContext.Set<UserDepartment>()
            .CountAsync(userDepartment => userDepartment.DepartmentId == id, cancellationToken);

    public void Add(Department department) => _dbContext.Set<Department>().Add(department);

    public void Remove(Department department) => _dbContext.Set<Department>().Remove(department);
}
