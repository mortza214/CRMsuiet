using CrmSuite.BuildingBlocks.Domain.Exceptions;
using CrmSuite.Modules.Foundation.Domain.Entities;
using CrmSuite.Modules.Payments.Domain.Enums;

namespace CrmSuite.Modules.Payments.Domain.Entities;

public sealed class Transaction : BaseEntity
{
    private Transaction()
    {
    }

    public Transaction(
        Guid? invoiceId,
        Guid customerId,
        decimal amount,
        PaymentMethod paymentMethod,
        PaymentGatewayName gatewayName,
        string? referenceNumber,
        string trackingCode,
        Guid? createdByUserId,
        DateTime createdAtUtc)
        : base(Guid.NewGuid())
    {
        if (amount <= 0)
        {
            throw new DomainException("مبلغ تراکنش باید بزرگ‌تر از صفر باشد.", "PAYMENTS_AMOUNT_INVALID");
        }

        if (string.IsNullOrWhiteSpace(trackingCode))
        {
            throw new DomainException("کد رهگیری تراکنش الزامی است.", "PAYMENTS_TRACKING_REQUIRED");
        }

        InvoiceId = invoiceId;
        CustomerId = customerId;
        Amount = amount;
        PaymentMethod = paymentMethod;
        GatewayName = gatewayName;
        ReferenceNumber = string.IsNullOrWhiteSpace(referenceNumber) ? null : referenceNumber.Trim();
        TrackingCode = trackingCode.Trim();
        Status = TransactionStatus.Pending;
        MarkCreated(createdByUserId, createdAtUtc);
    }

    public Guid? InvoiceId { get; private set; }

    public Guid CustomerId { get; private set; }

    public decimal Amount { get; private set; }

    public PaymentMethod PaymentMethod { get; private set; }

    public PaymentGatewayName GatewayName { get; private set; }

    public string? ReferenceNumber { get; private set; }

    public string TrackingCode { get; private set; } = string.Empty;

    public TransactionStatus Status { get; private set; }

    public DateTime? PaidAt { get; private set; }

    public string? GatewayResponse { get; private set; }

    public void MarkSuccess(
        string? referenceNumber,
        string? gatewayResponse,
        Guid? userId,
        DateTime utcNow)
    {
        if (Status != TransactionStatus.Pending)
        {
            throw new DomainException("فقط تراکنش در انتظار قابل تأیید است.", "PAYMENTS_STATUS_INVALID");
        }

        Status = TransactionStatus.Success;
        ReferenceNumber = string.IsNullOrWhiteSpace(referenceNumber)
            ? ReferenceNumber
            : referenceNumber.Trim();
        GatewayResponse = gatewayResponse;
        PaidAt = DateTime.SpecifyKind(utcNow, DateTimeKind.Utc);
        MarkUpdated(userId, utcNow);
    }

    public void MarkFailed(string? gatewayResponse, Guid? userId, DateTime utcNow)
    {
        if (Status != TransactionStatus.Pending)
        {
            throw new DomainException("فقط تراکنش در انتظار قابل ناموفق‌سازی است.", "PAYMENTS_STATUS_INVALID");
        }

        Status = TransactionStatus.Failed;
        GatewayResponse = gatewayResponse;
        MarkUpdated(userId, utcNow);
    }

    public void MarkRefunded(Guid? userId, DateTime utcNow)
    {
        if (Status != TransactionStatus.Success)
        {
            throw new DomainException("فقط تراکنش موفق قابل برگشت است.", "PAYMENTS_REFUND_STATUS_INVALID");
        }

        Status = TransactionStatus.Refunded;
        MarkUpdated(userId, utcNow);
    }
}
