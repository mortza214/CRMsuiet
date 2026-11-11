using CrmSuite.BuildingBlocks.Infrastructure.Persistence;
using CrmSuite.Modules.Identity.Application.Abstractions;
using CrmSuite.Modules.Identity.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace CrmSuite.Modules.Identity.Infrastructure.Persistence.Repositories;

public sealed class RoleRepository : IRoleRepository
{
    private readonly AppDbContext _dbContext;

    public RoleRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<Role?> GetByIdWithPermissionsAsync(Guid id, CancellationToken cancellationToken) =>
        WithPermissions().SingleOrDefaultAsync(role => role.Id == id, cancellationToken);

    public async Task<IReadOnlyList<Role>> GetAllAsync(
        bool includePermissions,
        CancellationToken cancellationToken)
    {
        var query = includePermissions ? WithPermissions().AsNoTracking() : _dbContext.Set<Role>().AsNoTracking();

        return await query
            .OrderBy(role => role.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Role>> GetByIdsAsync(
        IReadOnlyCollection<Guid> ids,
        bool includePermissions,
        CancellationToken cancellationToken)
    {
        if (ids.Count == 0)
        {
            return [];
        }

        var query = includePermissions ? WithPermissions() : _dbContext.Set<Role>();

        return await query
            .Where(role => ids.Contains(role.Id))
            .ToListAsync(cancellationToken);
    }

    public Task<bool> NameExistsAsync(
        string name,
        Guid? excludeId,
        CancellationToken cancellationToken)
    {
        var query = _dbContext.Set<Role>().Where(role => role.Name == name.Trim());

        if (excludeId.HasValue)
        {
            query = query.Where(role => role.Id != excludeId.Value);
        }

        return query.AnyAsync(cancellationToken);
    }

    public void Add(Role role) => _dbContext.Set<Role>().Add(role);

    public void Remove(Role role) => _dbContext.Set<Role>().Remove(role);

    private IQueryable<Role> WithPermissions() =>
        _dbContext.Set<Role>()
            .Include(role => role.RolePermissions)
            .ThenInclude(rolePermission => rolePermission.Permission);
}
