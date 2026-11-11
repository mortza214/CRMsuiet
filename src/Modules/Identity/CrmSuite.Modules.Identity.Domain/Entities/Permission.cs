using CrmSuite.BuildingBlocks.Domain.Exceptions;
using CrmSuite.Modules.Foundation.Domain.Entities;

namespace CrmSuite.Modules.Identity.Domain.Entities;

public sealed class Permission : BaseEntity
{
    private Permission()
    {
    }

    public Permission(string code, string group, string displayName)
        : base(Guid.NewGuid())
    {
        Code = NormalizeCode(code);
        Group = string.IsNullOrWhiteSpace(group) ? Code.Split('.')[0] : group.Trim();
        DisplayName = string.IsNullOrWhiteSpace(displayName) ? Code : displayName.Trim();
    }

    public string Code { get; private set; } = string.Empty;

    public string Group { get; private set; } = string.Empty;

    public string DisplayName { get; private set; } = string.Empty;

    public ICollection<RolePermission> RolePermissions { get; private set; } = new List<RolePermission>();

    private static string NormalizeCode(string code)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            throw new DomainException("کد Permission نمی‌تواند خالی باشد.", "IDENTITY_PERMISSION_CODE_REQUIRED");
        }

        var normalized = code.Trim();
        if (normalized.Length > 200)
        {
            throw new DomainException("کد Permission حداکثر ۲۰۰ کاراکتر است.", "IDENTITY_PERMISSION_CODE_TOO_LONG");
        }

        return normalized;
    }
}
