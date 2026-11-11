using CrmSuite.BuildingBlocks.Domain.Exceptions;
using CrmSuite.Modules.Catalog.Domain.Enums;
using CrmSuite.Modules.Foundation.Domain.Entities;

namespace CrmSuite.Modules.Catalog.Domain.Entities;

public sealed class DataCenterItem : BaseEntity
{
    private DataCenterItem()
    {
    }

    public DataCenterItem(
        DataCenterItemType itemType,
        string name,
        decimal price,
        ServiceUnit unit)
        : base(Guid.NewGuid())
    {
        ItemType = itemType;
        Name = NormalizeName(name);
        Price = NormalizePrice(price);
        Unit = unit;
    }

    public DataCenterItemType ItemType { get; private set; }

    public string Name { get; private set; } = string.Empty;

    public decimal Price { get; private set; }

    public ServiceUnit Unit { get; private set; }

    public void Update(
        DataCenterItemType itemType,
        string name,
        decimal price,
        ServiceUnit unit,
        DateTime utcNow,
        Guid? userId)
    {
        ItemType = itemType;
        Name = NormalizeName(name);
        Price = NormalizePrice(price);
        Unit = unit;
        MarkUpdated(userId, utcNow);
    }

    private static string NormalizeName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new DomainException("نام آیتم دیتاسنتر الزامی است.", "CATALOG_DC_ITEM_NAME_REQUIRED");
        }

        return name.Trim();
    }

    private static decimal NormalizePrice(decimal price)
    {
        if (price < 0)
        {
            throw new DomainException("قیمت آیتم دیتاسنتر نمی‌تواند منفی باشد.", "CATALOG_DC_ITEM_PRICE_INVALID");
        }

        return price;
    }
}
