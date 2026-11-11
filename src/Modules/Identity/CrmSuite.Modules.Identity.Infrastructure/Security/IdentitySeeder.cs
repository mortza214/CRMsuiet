using CrmSuite.BuildingBlocks.Application.Security;
using CrmSuite.BuildingBlocks.Infrastructure.Persistence;
using CrmSuite.Modules.Identity.Application.Abstractions;
using CrmSuite.Modules.Identity.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace CrmSuite.Modules.Identity.Infrastructure.Security;

/// <summary>
/// Idempotent seed for permissions, the 12 built-in roles, role-permission assignments, and
/// the initial SuperAdmin account. Must run after EF migrations.
/// </summary>
public sealed class IdentitySeeder
{
    public const string SuperAdminUsername = "superadmin";
    public const string SuperAdminDefaultPassword = "Admin@123";

    private static readonly IReadOnlyDictionary<string, string> RoleDisplayNames =
        new Dictionary<string, string>(StringComparer.Ordinal)
        {
            [Roles.SuperAdmin] = "مدیر کل",
            [Roles.SystemAdmin] = "مدیر سیستم",
            [Roles.SalesExpert] = "کارشناس فروش",
            [Roles.SalesManager] = "مدیر فروش",
            [Roles.TechnicalExpert] = "کارشناس فنی",
            [Roles.TechnicalManager] = "مدیر فنی",
            [Roles.PurchasingExpert] = "کارشناس خرید",
            [Roles.FinanceManager] = "مدیر مالی",
            [Roles.FinanceExpert] = "کارشناس مالی",
            [Roles.Support] = "پشتیبانی",
            [Roles.DataCenterSupport] = "پشتیبانی دیتاسنتر",
            [Roles.ContractsExpert] = "کارشناس قراردادها",
            [Roles.Installer] = "نیروی نصب",
            [Roles.Customer] = "مشتری پورتال",
            [Roles.Reseller] = "ریسلر",
        };

    private readonly AppDbContext _dbContext;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IConfiguration _configuration;
    private readonly IHostEnvironment _environment;
    private readonly ILogger<IdentitySeeder> _logger;

    public IdentitySeeder(
        AppDbContext dbContext,
        IPasswordHasher passwordHasher,
        IConfiguration configuration,
        IHostEnvironment environment,
        ILogger<IdentitySeeder> logger)
    {
        _dbContext = dbContext;
        _passwordHasher = passwordHasher;
        _configuration = configuration;
        _environment = environment;
        _logger = logger;
    }

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;

        var existingPermissionCodes = await _dbContext.Set<Permission>()
            .Select(permission => permission.Code)
            .ToListAsync(cancellationToken);

        var existingCodeSet = existingPermissionCodes.ToHashSet(StringComparer.Ordinal);
        var newPermissions = Permissions.All
            .Where(code => !existingCodeSet.Contains(code))
            .Select(code => new Permission(code, code.Split('.')[0], code))
            .ToArray();

        if (newPermissions.Length > 0)
        {
            await _dbContext.Set<Permission>().AddRangeAsync(newPermissions, cancellationToken);
            await _dbContext.SaveChangesAsync(cancellationToken);
        }

        var existingRoles = await _dbContext.Set<Role>()
            .ToDictionaryAsync(role => role.Name, StringComparer.Ordinal, cancellationToken);

        foreach (var roleName in Roles.All)
        {
            if (existingRoles.ContainsKey(roleName))
            {
                continue;
            }

            var role = new Role(
                roleName,
                RoleDisplayNames.TryGetValue(roleName, out var displayName) ? displayName : roleName);
            role.MarkCreated(null, now);
            _dbContext.Set<Role>().Add(role);
            existingRoles[roleName] = role;
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        var permissionsByCode = await _dbContext.Set<Permission>()
            .ToDictionaryAsync(permission => permission.Code, StringComparer.Ordinal, cancellationToken);

        var roleIds = existingRoles.Values.Select(role => role.Id).ToArray();
        var existingAssignments = await _dbContext.Set<RolePermission>()
            .Where(rolePermission => roleIds.Contains(rolePermission.RoleId))
            .Select(rolePermission => new { rolePermission.RoleId, rolePermission.PermissionId })
            .ToListAsync(cancellationToken);

        var assignmentKeys = existingAssignments
            .Select(item => (item.RoleId, item.PermissionId))
            .ToHashSet();

        foreach (var role in existingRoles.Values)
        {
            var configured = RolePermissionMap.For(role.Name);
            var codes = configured.Contains(Permissions.Wildcard, StringComparer.Ordinal)
                ? Permissions.All
                : configured;

            foreach (var code in codes)
            {
                if (!permissionsByCode.TryGetValue(code, out var permission))
                {
                    continue;
                }

                if (assignmentKeys.Add((role.Id, permission.Id)))
                {
                    _dbContext.Set<RolePermission>().Add(new RolePermission(role.Id, permission.Id));
                }
            }
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        var superAdminRole = existingRoles[Roles.SuperAdmin];
        var superAdmin = await _dbContext.Set<User>()
            .SingleOrDefaultAsync(user => user.Username == SuperAdminUsername, cancellationToken);

        if (superAdmin is null)
        {
            var password = _configuration["Identity:SuperAdminPassword"];

            if (string.IsNullOrWhiteSpace(password))
            {
                if (_environment.IsProduction())
                {
                    throw new InvalidOperationException(
                        "Identity:SuperAdminPassword must be configured before the first production seed.");
                }

                password = SuperAdminDefaultPassword;
            }

            superAdmin = new User(
                SuperAdminUsername,
                _passwordHasher.Hash(password),
                "مدیر کل سیستم",
                "09120000000",
                "superadmin@crmsuite.local");
            superAdmin.MarkCreated(null, now);
            _dbContext.Set<User>().Add(superAdmin);
            await _dbContext.SaveChangesAsync(cancellationToken);

            _logger.LogWarning(
                "Default SuperAdmin created with username '{Username}'. Change the default password immediately.",
                SuperAdminUsername);
        }

        var hasSuperAdminRole = await _dbContext.Set<UserRole>()
            .AnyAsync(
                userRole => userRole.UserId == superAdmin.Id && userRole.RoleId == superAdminRole.Id,
                cancellationToken);

        if (!hasSuperAdminRole)
        {
            _dbContext.Set<UserRole>().Add(new UserRole(superAdmin.Id, superAdminRole.Id));
            await _dbContext.SaveChangesAsync(cancellationToken);
        }

        _logger.LogInformation(
            "Identity seed completed. Permissions={PermissionCount}, Roles={RoleCount}, SuperAdmin={SuperAdminId}",
            Permissions.All.Count,
            Roles.All.Count,
            superAdmin.Id);
    }
}
