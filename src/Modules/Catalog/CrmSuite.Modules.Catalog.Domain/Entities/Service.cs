using CrmSuite.BuildingBlocks.Domain.Exceptions;
using CrmSuite.Modules.Catalog.Domain.Enums;
using CrmSuite.Modules.Foundation.Domain.Entities;

namespace CrmSuite.Modules.Catalog.Domain.Entities;

public sealed class Service : BaseEntity
{
    private Service()
    {
    }

    public Service(
        string code,
        string name,
        Guid categoryId,
        ServiceType serviceType,
        ServiceUnit unit,
        string? description,
        bool isActive = false)
        : base(Guid.NewGuid())
    {
        Code = NormalizeCode(code);
        Name = NormalizeName(name);
        CategoryId = categoryId;
        ServiceType = serviceType;
        Unit = unit;
        Description = NormalizeDescription(description);
        IsActive = isActive;
        PricingMode = PricingMode.UnitBased;
        BillingPeriod = serviceType == ServiceType.PhysicalProduct
            ? BillingPeriod.OneTime
            : BillingPeriod.Monthly;
    }

    public string Code { get; private set; } = string.Empty;

    public string Name { get; private set; } = string.Empty;

    public Guid CategoryId { get; private set; }

    public ServiceCategory? Category { get; private set; }

    public ServiceType ServiceType { get; private set; }

    public ServiceUnit Unit { get; private set; }

    public string? Description { get; private set; }

    public bool IsActive { get; private set; }

    public Guid? WorkflowDefinitionId { get; private set; }

    public PricingMode PricingMode { get; private set; } = PricingMode.UnitBased;

    public BillingPeriod BillingPeriod { get; private set; } = BillingPeriod.Monthly;

    public decimal? CommissionPercent { get; private set; }

    public string? AttributeSchemaJson { get; private set; }

    public ICollection<ServicePrice> Prices { get; private set; } = new List<ServicePrice>();

    public ServicePrice? CurrentOpenPrice =>
        Prices
            .Where(price => price.EffectiveTo is null)
            .OrderByDescending(price => price.EffectiveFrom)
            .FirstOrDefault();

    public ServicePrice? GetPriceAt(DateTime utcDate) =>
        Prices
            .Where(price => price.IsEffectiveOn(DateTime.SpecifyKind(utcDate, DateTimeKind.Utc)))
            .OrderByDescending(price => price.EffectiveFrom)
            .FirstOrDefault();

    public void Update(
        string code,
        string name,
        Guid categoryId,
        ServiceType serviceType,
        ServiceUnit unit,
        string? description,
        bool isActive,
        DateTime utcNow,
        Guid? userId)
    {
        if (isActive && PricingMode != PricingMode.Additive && Prices.Count == 0)
        {
            throw new DomainException(
                "سرویس بدون قیمت نمی‌تواند فعال شود.",
                "CATALOG_SERVICE_ACTIVE_WITHOUT_PRICE");
        }

        Code = NormalizeCode(code);
        Name = NormalizeName(name);
        CategoryId = categoryId;
        ServiceType = serviceType;
        Unit = unit;
        Description = NormalizeDescription(description);
        IsActive = isActive;
        MarkUpdated(userId, utcNow);
    }

    public void SetWorkflowDefinition(Guid? definitionId, Guid? userId, DateTime utcNow)
    {
        WorkflowDefinitionId = definitionId;
        MarkUpdated(userId, utcNow);
    }

    public void ConfigurePricing(
        PricingMode pricingMode,
        BillingPeriod billingPeriod,
        ServiceUnit unit,
        decimal? commissionPercent,
        string? attributeSchemaJson,
        Guid? userId,
        DateTime utcNow)
    {
        ValidatePricing(pricingMode, billingPeriod, unit, commissionPercent);

        PricingMode = pricingMode;
        BillingPeriod = billingPeriod;
        Unit = unit;
        CommissionPercent = commissionPercent;
        AttributeSchemaJson = NormalizeSchema(attributeSchemaJson);
        MarkUpdated(userId, utcNow);
    }

    public void SetAttributeSchema(
        string? attributeSchemaJson,
        Guid? userId,
        DateTime utcNow)
    {
        AttributeSchemaJson = NormalizeSchema(attributeSchemaJson);
        MarkUpdated(userId, utcNow);
    }

    public void EnsureCanBeEdited()
    {
        if (IsActive && Prices.Count == 0)
        {
            return;
        }
    }

    private static void ValidatePricing(
        PricingMode pricingMode,
        BillingPeriod billingPeriod,
        ServiceUnit unit,
        decimal? commissionPercent)
    {
        if (!Enum.IsDefined(pricingMode))
        {
            throw new DomainException("حالت قیمت‌گذاری معتبر نیست.", "CATALOG_PRICING_MODE_INVALID");
        }

        if (!Enum.IsDefined(billingPeriod))
        {
            throw new DomainException("دوره صورتحساب معتبر نیست.", "CATALOG_BILLING_PERIOD_INVALID");
        }

        if (pricingMode == PricingMode.UnitBased && !Enum.IsDefined(unit))
        {
            throw new DomainException("برای قیمت‌گذاری واحدی، واحد الزامی است.", "CATALOG_UNIT_REQUIRED");
        }

        if (commissionPercent is < 0 or > 100)
        {
            throw new DomainException(
                "درصد پورسانت باید بین صفر و ۱۰۰ باشد.",
                "CATALOG_COMMISSION_PERCENT_INVALID");
        }
    }

    private static string? NormalizeSchema(string? schemaJson) =>
        string.IsNullOrWhiteSpace(schemaJson) ? null : schemaJson.Trim();

    public void AddPrice(
        decimal price,
        DateTime effectiveFromUtc,
        DateTime utcNow,
        Guid? userId,
        string currency = "IRR")
    {
        if (price <= 0)
        {
            throw new DomainException("قیمت سرویس باید بزرگ‌تر از صفر باشد.", "CATALOG_PRICE_INVALID");
        }

        var effectiveFrom = DateTime.SpecifyKind(effectiveFromUtc, DateTimeKind.Utc);
        var openPrices = Prices
            .Where(priceRecord => priceRecord.EffectiveTo is null)
            .OrderByDescending(priceRecord => priceRecord.EffectiveFrom)
            .ToArray();

        if (openPrices.Length > 1)
        {
            throw new DomainException(
                "برای هر سرویس فقط یک قیمت باز مجاز است.",
                "CATALOG_MULTIPLE_OPEN_PRICES");
        }

        var currentOpen = openPrices.FirstOrDefault();

        if (currentOpen is not null)
        {
            if (effectiveFrom <= currentOpen.EffectiveFrom)
            {
                throw new DomainException(
                    "تاریخ اعتبار قیمت جدید باید بعد از قیمت جاری باشد.",
                    "CATALOG_PRICE_NOT_CHRONOLOGICAL");
            }

            currentOpen.Close(effectiveFrom);
        }

        Prices.Add(new ServicePrice(Id, price, effectiveFrom, utcNow, currency));
        MarkUpdated(userId, utcNow);
    }

    public void SetStatus(bool isActive, DateTime utcNow, Guid? userId)
    {
        if (isActive && PricingMode != PricingMode.Additive && Prices.Count == 0)
        {
            throw new DomainException(
                "سرویس بدون قیمت نمی‌تواند فعال شود.",
                "CATALOG_SERVICE_ACTIVE_WITHOUT_PRICE");
        }

        IsActive = isActive;
        MarkUpdated(userId, utcNow);
    }

    private static string NormalizeCode(string code)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            throw new DomainException("کد سرویس الزامی است.", "CATALOG_SERVICE_CODE_REQUIRED");
        }

        var normalized = code.Trim().ToUpperInvariant();
        if (!System.Text.RegularExpressions.Regex.IsMatch(normalized, "^[A-Z0-9._-]{2,50}$"))
        {
            throw new DomainException(
                "کد سرویس فقط می‌تواند شامل حروف لاتین، رقم، نقطه، خط تیره و زیرخط باشد.",
                "CATALOG_SERVICE_CODE_INVALID");
        }

        return normalized;
    }

    private static string NormalizeName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new DomainException("نام سرویس الزامی است.", "CATALOG_SERVICE_NAME_REQUIRED");
        }

        var normalized = name.Trim();
        return normalized.Length > 200 ? normalized[..200] : normalized;
    }

    private static string? NormalizeDescription(string? description) =>
        string.IsNullOrWhiteSpace(description) ? null : description.Trim();
}
