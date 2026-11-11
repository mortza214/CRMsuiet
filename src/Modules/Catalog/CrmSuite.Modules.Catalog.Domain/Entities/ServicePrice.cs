using CrmSuite.BuildingBlocks.Domain.Entities;
using CrmSuite.BuildingBlocks.Domain.Exceptions;

namespace CrmSuite.Modules.Catalog.Domain.Entities;

public sealed class ServicePrice : Entity<Guid>
{
    private ServicePrice()
    {
    }

    public ServicePrice(
        Guid serviceId,
        decimal price,
        DateTime effectiveFrom,
        DateTime createdAt,
        string currency = "IRR")
    {
        if (price <= 0)
        {
            throw new DomainException("قیمت سرویس باید بزرگ‌تر از صفر باشد.", "CATALOG_PRICE_INVALID");
        }

        Id = Guid.NewGuid();
        ServiceId = serviceId;
        Price = price;
        Currency = string.IsNullOrWhiteSpace(currency) ? "IRR" : currency.Trim().ToUpperInvariant();
        EffectiveFrom = DateTime.SpecifyKind(effectiveFrom, DateTimeKind.Utc);
        CreatedAt = DateTime.SpecifyKind(createdAt, DateTimeKind.Utc);
    }

    public Guid ServiceId { get; private set; }

    public decimal Price { get; private set; }

    public decimal UnitPrice => Price;

    public string Currency { get; private set; } = "IRR";

    public DateTime EffectiveFrom { get; private set; }

    public DateTime ValidFrom => EffectiveFrom;

    public DateTime? ValidTo => EffectiveTo;

    public DateTime? EffectiveTo { get; private set; }

    public DateTime CreatedAt { get; private set; }

    public void Close(DateTime effectiveTo)
    {
        var end = DateTime.SpecifyKind(effectiveTo, DateTimeKind.Utc);

        if (EffectiveTo.HasValue)
        {
            throw new DomainException("این قیمت قبلاً بسته شده است.", "CATALOG_PRICE_ALREADY_CLOSED");
        }

        if (end <= EffectiveFrom)
        {
            throw new DomainException(
                "تاریخ پایان قیمت باید بعد از تاریخ شروع باشد.",
                "CATALOG_PRICE_END_BEFORE_START");
        }

        EffectiveTo = end;
    }

    public bool IsEffectiveOn(DateTime utcDate) =>
        EffectiveFrom <= utcDate && (EffectiveTo is null || EffectiveTo > utcDate);
}
