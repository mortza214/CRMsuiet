using CrmSuite.BuildingBlocks.Domain.Exceptions;
using CrmSuite.Modules.Foundation.Domain.Entities;

namespace CrmSuite.Modules.Identity.Domain.Entities;

public sealed class Role : BaseEntity
{
    private Role()
    {
    }

    public Role(string name, string displayName)
        : base(Guid.NewGuid())
    {
        Name = NormalizeName(name);
        DisplayName = NormalizeDisplayName(displayName);
    }

    public string Name { get; private set; } = string.Empty;

    public string DisplayName { get; private set; } = string.Empty;

    public ICollection<RolePermission> RolePermissions { get; private set; } = new List<RolePermission>();

    public ICollection<UserRole> UserRoles { get; private set; } = new List<UserRole>();

    public void Rename(string name, string displayName, Guid? userId, DateTime utcNow)
    {
        Name = NormalizeName(name);
        DisplayName = NormalizeDisplayName(displayName);
        MarkUpdated(userId, utcNow);
    }

    public void ReplacePermissions(IEnumerable<Guid> permissionIds, Guid? userId, DateTime utcNow)
    {
        var distinctPermissionIds = permissionIds.Distinct().ToArray();
        RolePermissions.Clear();

        foreach (var permissionId in distinctPermissionIds)
        {
            RolePermissions.Add(new RolePermission(Id, permissionId));
        }

        MarkUpdated(userId, utcNow);
    }

    private static string NormalizeName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new DomainException("نام نقش نمی‌تواند خالی باشد.", "IDENTITY_ROLE_NAME_REQUIRED");
        }

        var normalized = name.Trim();
        if (normalized.Length > 100)
        {
            throw new DomainException("نام نقش حداکثر ۱۰۰ کاراکتر است.", "IDENTITY_ROLE_NAME_TOO_LONG");
        }

        return normalized;
    }

    private static string NormalizeDisplayName(string displayName)
    {
        if (string.IsNullOrWhiteSpace(displayName))
        {
            throw new DomainException("عنوان نقش نمی‌تواند خالی باشد.", "IDENTITY_ROLE_DISPLAY_REQUIRED");
        }

        var normalized = displayName.Trim();
        return normalized.Length > 200 ? normalized[..200] : normalized;
    }
}
