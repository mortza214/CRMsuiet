using CrmSuite.BuildingBlocks.Infrastructure.Persistence;
using CrmSuite.Modules.Identity.Application.Abstractions;
using CrmSuite.Modules.Identity.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace CrmSuite.Modules.Identity.Infrastructure.Persistence.Repositories;

public sealed class PermissionRepository : IPermissionRepository
{
    private readonly AppDbContext _dbContext;

    public PermissionRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<Permission>> GetAllAsync(CancellationToken cancellationToken) =>
        await _dbContext.Set<Permission>()
            .AsNoTracking()
            .OrderBy(permission => permission.Group)
            .ThenBy(permission => permission.Code)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<Permission>> GetByIdsAsync(
        IReadOnlyCollection<Guid> ids,
        CancellationToken cancellationToken)
    {
        if (ids.Count == 0)
        {
            return [];
        }

        return await _dbContext.Set<Permission>()
            .Where(permission => ids.Contains(permission.Id))
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Permission>> GetByCodesAsync(
        IReadOnlyCollection<string> codes,
        CancellationToken cancellationToken)
    {
        if (codes.Count == 0)
        {
            return [];
        }

        return await _dbContext.Set<Permission>()
            .Where(permission => codes.Contains(permission.Code))
            .ToListAsync(cancellationToken);
    }

    public async Task AddRangeAsync(
        IEnumerable<Permission> permissions,
        CancellationToken cancellationToken)
    {
        await _dbContext.Set<Permission>().AddRangeAsync(permissions, cancellationToken);
    }
}
