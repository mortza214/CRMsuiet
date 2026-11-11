using CrmSuite.BuildingBlocks.Domain.Exceptions;
using CrmSuite.Modules.Catalog.Domain.Enums;
using CrmSuite.Modules.Foundation.Domain.Entities;

namespace CrmSuite.Modules.Catalog.Domain.Entities;

public sealed class ServiceAddon : BaseEntity
{
    private ServiceAddon()
    {
    }

    /// <summary>Legacy constructor retained for existing callers and tests.</summary>
    public ServiceAddon(string name, decimal price, ServiceUnit unit, bool isOptional)
        : base(Guid.NewGuid())
    {
        Name = NormalizeName(name);
        Price = NormalizePrice(price);
        Unit = unit;
        IsOptional = isOptional;
        Code = $"ADD-{Id:N}"[..12].ToUpperInvariant();
        PricingMode = AddonPricingMode.Fixed;
        IsActive = true;
        SortOrder = 0;
        Description = null;
        ServiceId = null;
        MarkCreated(null, DateTime.UtcNow);
    }

    public ServiceAddon(
        Guid? serviceId,
        string code,
        string name,
        AddonPricingMode pricingMode,
        ServiceUnit unit,
        decimal unitPrice,
        bool isActive,
        int sortOrder,
        string? description,
        Guid? createdByUserId,
        DateTime createdAtUtc)
        : base(Guid.NewGuid())
    {
        ServiceId = serviceId;
        Code = NormalizeCode(code);
        Name = NormalizeName(name);
        PricingMode = pricingMode;
        Unit = unit;
        Price = NormalizePrice(unitPrice);
        IsActive = isActive;
        IsOptional = true;
        SortOrder = sortOrder < 0 ? 0 : sortOrder;
        Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();
        MarkCreated(createdByUserId, createdAtUtc);
    }

    public Guid? ServiceId { get; private set; }

    public string Code { get; private set; } = string.Empty;

    public string Name { get; private set; } = string.Empty;

    public AddonPricingMode PricingMode { get; private set; } = AddonPricingMode.Fixed;

    public decimal Price { get; private set; }

    public decimal UnitPrice => Price;

    public ServiceUnit Unit { get; private set; }

    public bool IsActive { get; private set; } = true;

    public bool IsOptional { get; private set; }

    public int SortOrder { get; private set; }

    public string? Description { get; private set; }

    public bool IsGlobal => !ServiceId.HasValue;

    public void Update(
        string name,
        decimal price,
        ServiceUnit unit,
        bool isOptional,
        DateTime utcNow,
        Guid? userId)
    {
        Name = NormalizeName(name);
        Price = NormalizePrice(price);
        Unit = unit;
        IsOptional = isOptional;
        MarkUpdated(userId, utcNow);
    }

    public void UpdateDetails(
        Guid? serviceId,
        string code,
        string name,
        AddonPricingMode pricingMode,
        ServiceUnit unit,
        decimal unitPrice,
        bool isActive,
        int sortOrder,
        string? description,
        Guid? userId,
        DateTime utcNow)
    {
        ServiceId = serviceId;
        Code = NormalizeCode(code);
        Name = NormalizeName(name);
        PricingMode = pricingMode;
        Unit = unit;
        Price = NormalizePrice(unitPrice);
        IsActive = isActive;
        SortOrder = sortOrder < 0 ? 0 : sortOrder;
        Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();
        MarkUpdated(userId, utcNow);
    }

    public void Deactivate(Guid? userId, DateTime utcNow)
    {
        IsActive = false;
        IsOptional = true;
        MarkUpdated(userId, utcNow);
    }

    public void SetService(Guid? serviceId, Guid? userId, DateTime utcNow)
    {
        ServiceId = serviceId;
        MarkUpdated(userId, utcNow);
    }

    private static string NormalizeCode(string code)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            throw new DomainException("کد آیتم جانبی الزامی است.", "CATALOG_ADDON_CODE_REQUIRED");
        }

        var normalized = code.Trim().ToUpperInvariant();
        if (normalized.Length > 50)
        {
            throw new DomainException("کد آیتم جانبی حداکثر ۵۰ کاراکتر است.", "CATALOG_ADDON_CODE_INVALID");
        }

        return normalized;
    }

    private static string NormalizeName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new DomainException("نام آیتم جانبی الزامی است.", "CATALOG_ADDON_NAME_REQUIRED");
        }

        var normalized = name.Trim();
        return normalized.Length > 200 ? normalized[..200] : normalized;
    }

    private static decimal NormalizePrice(decimal price)
    {
        if (price < 0)
        {
            throw new DomainException("قیمت آیتم جانبی نمی‌تواند منفی باشد.", "CATALOG_ADDON_PRICE_INVALID");
        }

        return price;
    }
}
