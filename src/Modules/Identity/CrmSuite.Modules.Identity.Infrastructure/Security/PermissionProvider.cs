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

        // Sales department membership permits intake and tracking, independently of a user's other role.
        var isSalesMember = await _dbContext.Set<UserDepartment>().AnyAsync(item =>
            item.UserId == userId && item.Department.Name == "فروش", cancellationToken);
        if (isSalesMember)
        {
            permissions.AddRange(new[] {
                Permissions.Customers.Customer.Read, Permissions.Orders.Order.Create, Permissions.Orders.Order.Read,
                Permissions.Workflow.Definition.Read, Permissions.Workflow.Instance.Read, Permissions.Workflow.Instance.Execute,
                Permissions.Workflow.Task.Read, Permissions.Forms.Form.Read, Permissions.Forms.Submission.Read,
                Permissions.Forms.Submission.Create });
            permissions = permissions.Distinct(StringComparer.Ordinal).OrderBy(code => code).ToList();
        }

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
