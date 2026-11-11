using CrmSuite.BuildingBlocks.Domain.Exceptions;
using CrmSuite.Modules.Foundation.Domain.Entities;

namespace CrmSuite.Modules.Identity.Domain.Entities;

public sealed class Department : BaseEntity
{
    private Department()
    {
    }

    public Department(string name, Guid? parentId = null)
        : base(Guid.NewGuid())
    {
        Name = NormalizeName(name);
        ParentId = parentId;
    }

    public string Name { get; private set; } = string.Empty;

    public Guid? ParentId { get; private set; }

    public ICollection<UserDepartment> UserDepartments { get; private set; } = new List<UserDepartment>();

    public void Rename(string name, Guid? parentId, Guid? userId, DateTime utcNow)
    {
        if (parentId == Id)
        {
            throw new DomainException("دپارتمان نمی‌تواند والد خودش باشد.", "IDENTITY_DEPARTMENT_SELF_PARENT");
        }

        Name = NormalizeName(name);
        ParentId = parentId;
        MarkUpdated(userId, utcNow);
    }

    private static string NormalizeName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new DomainException("نام دپارتمان نمی‌تواند خالی باشد.", "IDENTITY_DEPARTMENT_NAME_REQUIRED");
        }

        var normalized = name.Trim();
        return normalized.Length > 200 ? normalized[..200] : normalized;
    }
}
