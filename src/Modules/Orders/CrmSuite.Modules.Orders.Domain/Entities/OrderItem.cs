using CrmSuite.BuildingBlocks.Domain.Exceptions;
using CrmSuite.Modules.Foundation.Domain.Entities;
using CrmSuite.Modules.Orders.Domain.Enums;

namespace CrmSuite.Modules.Orders.Domain.Entities;

public sealed class OrderItem : BaseEntity
{
    private OrderItem()
    {
    }

    /// <summary>Legacy constructor retained for existing callers and tests.</summary>
    public OrderItem(
        Guid orderId,
        Guid? serviceId,
        Guid? dataCenterItemId,
        Guid? addonId,
        string itemName,
        int quantity,
        decimal unitPrice,
        BillingCycle billingCycle,
        DateTime createdAtUtc)
        : base(Guid.NewGuid())
    {
        ValidateSource(serviceId, dataCenterItemId, addonId);
        ValidateQuantity(quantity);

        if (unitPrice < 0)
        {
            throw new DomainException("قیمت واحد نمی‌تواند منفی باشد.", "ORDERS_ITEM_PRICE_INVALID");
        }

        OrderId = orderId;
        ServiceId = serviceId;
        DataCenterItemId = dataCenterItemId;
        AddonId = addonId;
        ItemName = NormalizeName(itemName);
        Quantity = quantity;
        QuantityDecimal = quantity;
        UnitPrice = unitPrice;
        UnitPriceSnapshot = unitPrice;
        AddonsTotal = 0;
        AddonsJson = null;
        AttributesJson = null;
        BillingCycle = billingCycle;
        TotalPrice = EffectivePrice;
        MarkCreated(null, createdAtUtc);
    }

    public OrderItem(
        Guid orderId,
        Guid serviceId,
        string itemName,
        int quantity,
        decimal unitPriceSnapshot,
        decimal addonsTotal,
        string? addonsJson,
        string? attributesJson,
        BillingCycle billingCycle,
        DateTime createdAtUtc,
        Guid? createdByUserId = null)
        : base(Guid.NewGuid())
    {
        ValidateSource(serviceId, null, null);
        ValidateQuantity(quantity);

        if (unitPriceSnapshot < 0 || addonsTotal < 0)
        {
            throw new DomainException("قیمت واحد یا آیتم‌های جانبی معتبر نیست.", "ORDERS_ITEM_PRICE_INVALID");
        }

        OrderId = orderId;
        ServiceId = serviceId;
        ItemName = NormalizeName(itemName);
        Quantity = quantity;
        QuantityDecimal = quantity;
        UnitPrice = unitPriceSnapshot;
        UnitPriceSnapshot = unitPriceSnapshot;
        AddonsTotal = addonsTotal;
        AddonsJson = string.IsNullOrWhiteSpace(addonsJson) ? null : addonsJson;
        AttributesJson = string.IsNullOrWhiteSpace(attributesJson) ? null : attributesJson;
        BillingCycle = billingCycle;
        TotalPrice = EffectivePrice;
        MarkCreated(createdByUserId, createdAtUtc);
    }

    public Guid OrderId { get; private set; }

    public Guid? ServiceId { get; private set; }

    public Guid? DataCenterItemId { get; private set; }

    public Guid? AddonId { get; private set; }

    public string ItemName { get; private set; } = string.Empty;

    public int Quantity { get; private set; }

    public decimal QuantityValue => Quantity;

    public decimal QuantityDecimal { get; private set; }

    public decimal UnitPrice { get; private set; }

    public decimal UnitPriceSnapshot { get; private set; }

    public decimal AddonsTotal { get; private set; }

    public string? AddonsJson { get; private set; }

    public string? AttributesJson { get; private set; }

    public decimal? OverridePrice { get; private set; }

    public string? OverrideReason { get; private set; }

    public Guid? OverrideBy { get; private set; }

    public decimal EffectivePrice =>
        (OverridePrice ?? UnitPriceSnapshot) * Quantity + AddonsTotal;

    public decimal CommissionBase => EffectivePrice;

    public decimal TotalPrice { get; private set; }

    public BillingCycle BillingCycle { get; private set; }

    public void OverrideUnitPrice(
        decimal overridePrice,
        string? reason,
        Guid? userId,
        DateTime utcNow)
    {
        if (overridePrice < 0)
        {
            throw new DomainException("قیمت جایگزین نمی‌تواند منفی باشد.", "ORDERS_OVERRIDE_PRICE_INVALID");
        }

        if (string.IsNullOrWhiteSpace(reason))
        {
            throw new DomainException("دلیل تغییر قیمت الزامی است.", "ORDERS_OVERRIDE_REASON_REQUIRED");
        }

        OverridePrice = overridePrice;
        OverrideReason = reason.Trim();
        OverrideBy = userId;
        TotalPrice = EffectivePrice;
        MarkUpdated(userId, utcNow);
    }

    public void UpdateAttributes(string? attributesJson, Guid? userId, DateTime utcNow)
    {
        AttributesJson = string.IsNullOrWhiteSpace(attributesJson) ? null : attributesJson;
        MarkUpdated(userId, utcNow);
    }

    public void UpdateAddonSnapshot(
        decimal addonsTotal,
        string? addonsJson,
        Guid? userId,
        DateTime utcNow)
    {
        if (addonsTotal < 0)
        {
            throw new DomainException("جمع آیتم‌های جانبی معتبر نیست.", "ORDERS_ADDONS_TOTAL_INVALID");
        }

        AddonsTotal = addonsTotal;
        AddonsJson = string.IsNullOrWhiteSpace(addonsJson) ? null : addonsJson;
        TotalPrice = EffectivePrice;
        MarkUpdated(userId, utcNow);
    }

    private static void ValidateSource(
        Guid? serviceId,
        Guid? dataCenterItemId,
        Guid? addonId)
    {
        var sources = new[] { serviceId, dataCenterItemId, addonId }.Count(id => id.HasValue);
        if (sources != 1)
        {
            throw new DomainException(
                "هر آیتم سفارش باید دقیقاً یکی از سرویس، آیتم دیتاسنتر یا آیتم جانبی باشد.",
                "ORDERS_ITEM_SOURCE_INVALID");
        }
    }

    private static void ValidateQuantity(int quantity)
    {
        if (quantity <= 0)
        {
            throw new DomainException("تعداد آیتم باید بزرگ‌تر از صفر باشد.", "ORDERS_ITEM_QUANTITY_INVALID");
        }
    }

    private static string NormalizeName(string itemName) =>
        string.IsNullOrWhiteSpace(itemName) ? "آیتم سفارش" : itemName.Trim();
}
