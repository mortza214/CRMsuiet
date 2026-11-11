using CrmSuite.BuildingBlocks.Domain.Exceptions;
using CrmSuite.Modules.Foundation.Domain.Entities;

namespace CrmSuite.Modules.Catalog.Domain.Entities;

public sealed class ServiceCategory : BaseEntity
{
    private ServiceCategory()
    {
    }

    public ServiceCategory(string name, Guid? parentId = null)
        : base(Guid.NewGuid())
    {
        Name = NormalizeName(name);
        ParentId = parentId;
    }

    public string Name { get; private set; } = string.Empty;

    public Guid? ParentId { get; private set; }

    public void Rename(string name, Guid? parentId, DateTime utcNow, Guid? userId)
    {
        if (parentId == Id)
        {
            throw new DomainException("دسته‌بندی نمی‌تواند والد خودش باشد.", "CATALOG_CATEGORY_SELF_PARENT");
        }

        Name = NormalizeName(name);
        ParentId = parentId;
        MarkUpdated(userId, utcNow);
    }

    private static string NormalizeName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new DomainException("نام دسته‌بندی الزامی است.", "CATALOG_CATEGORY_NAME_REQUIRED");
        }

        var normalized = name.Trim();
        return normalized.Length > 150 ? normalized[..150] : normalized;
    }
}
