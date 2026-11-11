using CrmSuite.BuildingBlocks.Domain.Exceptions;
using CrmSuite.Modules.Foundation.Domain.Entities;

namespace CrmSuite.Modules.Customers.Domain.Entities;

public sealed class CustomerTag : BaseEntity
{
    private CustomerTag()
    {
    }

    public CustomerTag(string name, string color)
        : base(Guid.NewGuid())
    {
        Name = NormalizeName(name);
        Color = NormalizeColor(color);
    }

    public string Name { get; private set; } = string.Empty;

    public string Color { get; private set; } = "#1677ff";

    public void Update(string name, string color, DateTime utcNow, Guid? userId)
    {
        Name = NormalizeName(name);
        Color = NormalizeColor(color);
        MarkUpdated(userId, utcNow);
    }

    private static string NormalizeName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new DomainException("نام تگ الزامی است.", "CUSTOMERS_TAG_NAME_REQUIRED");
        }

        var normalized = name.Trim();
        return normalized.Length > 100 ? normalized[..100] : normalized;
    }

    private static string NormalizeColor(string color)
    {
        var normalized = string.IsNullOrWhiteSpace(color) ? "#1677ff" : color.Trim();
        if (!System.Text.RegularExpressions.Regex.IsMatch(normalized, "^#[0-9A-Fa-f]{6}$"))
        {
            throw new DomainException("رنگ تگ باید با فرمت #RRGGBB باشد.", "CUSTOMERS_TAG_COLOR_INVALID");
        }

        return normalized;
    }
}
