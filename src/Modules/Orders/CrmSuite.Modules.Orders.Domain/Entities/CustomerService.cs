using CrmSuite.BuildingBlocks.Domain.Exceptions;
using CrmSuite.Modules.Foundation.Domain.Entities;
using CrmSuite.Modules.Orders.Domain.Enums;

namespace CrmSuite.Modules.Orders.Domain.Entities;

/// <summary>An activated service instance owned by a customer.</summary>
public sealed class CustomerService : BaseEntity
{
    private CustomerService()
    {
    }

    public CustomerService(
        Guid customerId,
        Guid serviceId,
        Guid orderId,
        Guid orderItemId,
        string serviceName,
        decimal unitPriceSnapshot,
        int quantity,
        decimal addonsTotal,
        string? attributesJson,
        decimal? commissionPercent,
        DateTime activatedAtUtc,
        DateTime nextInvoiceAtUtc,
        Guid? createdByUserId)
        : base(Guid.NewGuid())
    {
        if (customerId == Guid.Empty || serviceId == Guid.Empty || orderId == Guid.Empty)
        {
            throw new DomainException("شناسه‌های سرویس فعال معتبر نیستند.", "ORDERS_CUSTOMER_SERVICE_ID_INVALID");
        }

        if (unitPriceSnapshot < 0 || addonsTotal < 0 || quantity <= 0)
        {
            throw new DomainException("مقادیر سرویس فعال معتبر نیستند.", "ORDERS_CUSTOMER_SERVICE_AMOUNT_INVALID");
        }

        CustomerId = customerId;
        ServiceId = serviceId;
        OrderId = orderId;
        OrderItemId = orderItemId;
        ServiceName = string.IsNullOrWhiteSpace(serviceName) ? "سرویس" : serviceName.Trim();
        UnitPriceSnapshot = unitPriceSnapshot;
        Quantity = quantity;
        AddonsTotal = addonsTotal;
        AttributesJson = string.IsNullOrWhiteSpace(attributesJson) ? null : attributesJson;
        CommissionPercent = commissionPercent;
        CommissionBase = MonthlyAmount;
        Status = CustomerServiceStatus.Active;
        ActivatedAt = DateTime.SpecifyKind(activatedAtUtc, DateTimeKind.Utc);
        NextInvoiceAt = DateTime.SpecifyKind(nextInvoiceAtUtc, DateTimeKind.Utc);
        MarkCreated(createdByUserId, activatedAtUtc);
    }

    public Guid CustomerId { get; private set; }

    public Guid ServiceId { get; private set; }

    public Guid OrderId { get; private set; }

    public Guid OrderItemId { get; private set; }

    public string ServiceName { get; private set; } = string.Empty;

    public decimal UnitPriceSnapshot { get; private set; }

    public int Quantity { get; private set; }

    public decimal AddonsTotal { get; private set; }

    public decimal MonthlyAmount => UnitPriceSnapshot * Quantity + AddonsTotal;

    public string? AttributesJson { get; private set; }

    public decimal CommissionBase { get; private set; }

    public decimal? CommissionPercent { get; private set; }

    public CustomerServiceStatus Status { get; private set; }

    public DateTime ActivatedAt { get; private set; }

    public DateTime? NextInvoiceAt { get; private set; }

    public DateTime? LastInvoicedAt { get; private set; }

    public bool IsActive => Status == CustomerServiceStatus.Active;

    public void MarkInvoiced(DateTime invoicedAtUtc, DateTime nextInvoiceAtUtc, Guid? userId)
    {
        LastInvoicedAt = DateTime.SpecifyKind(invoicedAtUtc, DateTimeKind.Utc);
        NextInvoiceAt = DateTime.SpecifyKind(nextInvoiceAtUtc, DateTimeKind.Utc);
        MarkUpdated(userId, invoicedAtUtc);
    }

    public void Suspend(Guid? userId, DateTime utcNow)
    {
        Status = CustomerServiceStatus.Suspended;
        MarkUpdated(userId, utcNow);
    }

    public void Collect(Guid? userId, DateTime utcNow)
    {
        Status = CustomerServiceStatus.Collected;
        NextInvoiceAt = null;
        MarkUpdated(userId, utcNow);
    }

    public void AdjustQuantity(int quantity, Guid? userId, DateTime utcNow)
    {
        if (quantity <= 0)
        {
            throw new DomainException("تعداد/پهنای باند باید بزرگ‌تر از صفر باشد.", "ORDERS_CUSTOMER_SERVICE_QUANTITY_INVALID");
        }

        Quantity = quantity;
        MarkUpdated(userId, utcNow);
    }

    public void Cancel(Guid? userId, DateTime utcNow)
    {
        Status = CustomerServiceStatus.Canceled;
        NextInvoiceAt = null;
        MarkUpdated(userId, utcNow);
    }
}
