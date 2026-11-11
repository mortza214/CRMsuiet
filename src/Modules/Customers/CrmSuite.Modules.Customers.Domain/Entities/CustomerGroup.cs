using CrmSuite.BuildingBlocks.Domain.Exceptions;
using CrmSuite.Modules.Foundation.Domain.Entities;

namespace CrmSuite.Modules.Customers.Domain.Entities;

public sealed class CustomerGroup : BaseEntity
{
    private CustomerGroup()
    {
    }

    public CustomerGroup(string name, string? description)
        : base(Guid.NewGuid())
    {
        Name = NormalizeName(name);
        Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();
    }

    public string Name { get; private set; } = string.Empty;

    public string? Description { get; private set; }

    public void Update(string name, string? description, DateTime utcNow, Guid? userId)
    {
        Name = NormalizeName(name);
        Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();
        MarkUpdated(userId, utcNow);
    }

    private static string NormalizeName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new DomainException("نام گروه مشتری الزامی است.", "CUSTOMERS_GROUP_NAME_REQUIRED");
        }

        var normalized = name.Trim();
        return normalized.Length > 100 ? normalized[..100] : normalized;
    }
}
