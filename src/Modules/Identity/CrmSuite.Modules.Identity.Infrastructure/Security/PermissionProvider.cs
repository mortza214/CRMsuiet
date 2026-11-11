using CrmSuite.BuildingBlocks.Application.Security;
using CrmSuite.BuildingBlocks.Infrastructure.Persistence;
using CrmSuite.Modules.Identity.Application.Abstractions;
using CrmSuite.Modules.Identity.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace CrmSuite.Modules.Identity.Infrastructure.Security;

public sealed class PermissionProvider : IPermissionProvider
{
    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(3);
    private readonly AppDbContext _dbContext;
    private readonly IMemoryCache _cache;

    public PermissionProvider(AppDbContext dbContext, IMemoryCache cache)
    {
        _dbContext = dbContext;
        _cache = cache;
    }

    public async Task<IReadOnlyList<string>> GetPermissionsAsync(
        Guid userId,
        CancellationToken cancellationToken)
    {
        var cacheKey = CacheKey(userId);

        if (_cache.TryGetValue(cacheKey, out IReadOnlyList<string>? cached) && cached is not null)
        {
            return cached;
        }

        var roleNames = await _dbContext.Set<UserRole>()
            .Where(userRole => userRole.UserId == userId)
            .Select(userRole => userRole.Role.Name)
            .ToListAsync(cancellationToken);

        var permissions = await _dbContext.Set<RolePermission>()
            .Where(rolePermission => rolePermission.Role.UserRoles
                .Any(userRole => userRole.UserId == userId))
            .Select(rolePermission => rolePermission.Permission.Code)
            .Distinct()
            .OrderBy(code => code)
            .ToListAsync(cancellationToken);

        if (roleNames.Contains(Roles.SuperAdmin, StringComparer.Ordinal))
        {
            permissions.Add(Permissions.Wildcard);
        }

        _cache.Set(cacheKey, permissions, CacheDuration);
        return permissions;
    }

    public void Invalidate(Guid userId) => _cache.Remove(CacheKey(userId));

    private static string CacheKey(Guid userId) => $"identity:permissions:{userId:N}";
}
