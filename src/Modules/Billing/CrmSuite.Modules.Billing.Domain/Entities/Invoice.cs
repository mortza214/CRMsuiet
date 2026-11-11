using CrmSuite.BuildingBlocks.Domain.Exceptions;
using CrmSuite.Modules.Billing.Domain.Enums;
using CrmSuite.Modules.Foundation.Domain.Entities;

namespace CrmSuite.Modules.Billing.Domain.Entities;

public sealed class Invoice : BaseEntity
{
    private Invoice()
    {
    }

    public Invoice(
        string invoiceNumber,
        Guid orderId,
        Guid customerId,
        BillingInvoiceType type,
        DateTime issueDateUtc,
        DateTime dueDateUtc,
        decimal discountAmount,
        Guid? createdByUserId,
        DateTime createdAtUtc)
        : base(Guid.NewGuid())
    {
        InvoiceNumber = NormalizeNumber(invoiceNumber);
        OrderId = orderId;
        CustomerId = customerId;
        Type = type;
        IssueDate = DateTime.SpecifyKind(issueDateUtc, DateTimeKind.Utc);
        DueDate = DateTime.SpecifyKind(dueDateUtc, DateTimeKind.Utc);
        DiscountAmount = discountAmount < 0 ? throw new DomainException("تخفیف نمی‌تواند منفی باشد.", "BILLING_DISCOUNT_INVALID") : discountAmount;
        Status = InvoiceStatus.Draft;
        MarkCreated(createdByUserId, createdAtUtc);
    }

    public string InvoiceNumber { get; private set; } = string.Empty;

    public Guid OrderId { get; private set; }

    public Guid CustomerId { get; private set; }

    public BillingInvoiceType Type { get; private set; }

    public DateTime IssueDate { get; private set; }

    public DateTime DueDate { get; private set; }

    public decimal SubTotal { get; private set; }

    public decimal DiscountAmount { get; private set; }

    public decimal TaxAmount { get; private set; }

    public decimal InsuranceAmount { get; private set; }

    public decimal TotalAmount { get; private set; }

    public decimal PaidAmount { get; private set; }

    public InvoiceStatus Status { get; private set; }

    public string? PaymentMode { get; private set; }

    public bool RequiresPayment { get; private set; } = true;

    public ICollection<InvoiceItem> Items { get; private set; } = new List<InvoiceItem>();

    public void AddItem(InvoiceItem item)
    {
        if (item.InvoiceId != Id)
        {
            throw new DomainException("آیتم به فاکتور جاری تعلق ندارد.", "BILLING_ITEM_INVOICE_MISMATCH");
        }

        if (Status != InvoiceStatus.Draft)
        {
            throw new DomainException("فاکتور صادرشده قابل تغییر نیست.", "BILLING_INVOICE_LOCKED");
        }

        Items.Add(item);
    }

    public void RecalculateDraftTotal()
    {
        if (Status != InvoiceStatus.Draft)
        {
            throw new DomainException("فاکتور صادرشده قابل محاسبه مجدد پیش‌نویس نیست.", "BILLING_INVOICE_LOCKED");
        }

        SubTotal = Items.Sum(item => item.TotalPrice);
        TotalAmount = SubTotal - DiscountAmount + TaxAmount + InsuranceAmount;
    }

    public void SetPaymentTerms(string? paymentMode, bool requiresPayment)
    {
        if (Status != InvoiceStatus.Draft)
        {
            throw new DomainException("شرایط پرداخت فقط برای پیش‌نویس قابل تغییر است.", "BILLING_INVOICE_LOCKED");
        }

        PaymentMode = string.IsNullOrWhiteSpace(paymentMode)
            ? null
            : paymentMode.Trim();
        RequiresPayment = requiresPayment;
        MarkUpdated(null, DateTime.UtcNow);
    }

    public void Issue(
        decimal vatPercentage,
        decimal insurancePercentage,
        Guid? userId,
        DateTime utcNow)
    {
        if (Status != InvoiceStatus.Draft)
        {
            throw new DomainException("فاکتور قبلاً صادر شده است.", "BILLING_INVOICE_ALREADY_ISSUED");
        }

        if (vatPercentage < 0 || insurancePercentage < 0)
        {
            throw new DomainException("درصد مالیات یا بیمه معتبر نیست.", "BILLING_PERCENTAGE_INVALID");
        }

        SubTotal = Items.Sum(item => item.TotalPrice);
        var taxableBase = SubTotal - DiscountAmount;
        TaxAmount = decimal.Round(taxableBase * vatPercentage / 100m, 2, MidpointRounding.AwayFromZero);
        InsuranceAmount = decimal.Round(taxableBase * insurancePercentage / 100m, 2, MidpointRounding.AwayFromZero);
        TotalAmount = SubTotal - DiscountAmount + TaxAmount + InsuranceAmount;
        Status = InvoiceStatus.Issued;
        MarkUpdated(userId, utcNow);
    }

    public void RegisterPayment(decimal amount, Guid? userId, DateTime utcNow)
    {
        if (Status is InvoiceStatus.Draft or InvoiceStatus.Canceled or InvoiceStatus.Paid)
        {
            throw new DomainException(
                "فاکتور در وضعیت جاری قابل ثبت پرداخت نیست.",
                "BILLING_INVOICE_PAYMENT_STATUS_INVALID");
        }

        if (amount <= 0)
        {
            throw new DomainException("مبلغ پرداخت باید بزرگ‌تر از صفر باشد.", "BILLING_PAYMENT_AMOUNT_INVALID");
        }

        if (PaidAmount + amount > TotalAmount)
        {
            throw new DomainException("مبلغ پرداخت بیش از مانده فاکتور است.", "BILLING_PAYMENT_EXCEEDS_TOTAL");
        }

        PaidAmount += amount;
        Status = PaidAmount >= TotalAmount ? InvoiceStatus.Paid : InvoiceStatus.PartiallyPaid;
        MarkUpdated(userId, utcNow);
    }

    public void UpdateStatus(InvoiceStatus newStatus, Guid? userId, DateTime utcNow)
    {
        if (newStatus == Status)
        {
            return;
        }

        if (Status is InvoiceStatus.Paid or InvoiceStatus.Canceled)
        {
            throw new DomainException("فاکتور تسویه‌شده یا لغوشده قابل تغییر نیست.", "BILLING_INVOICE_FINALIZED");
        }

        var allowed = (Status, newStatus) switch
        {
            (InvoiceStatus.Draft, InvoiceStatus.Canceled) => true,
            (InvoiceStatus.Issued, InvoiceStatus.PartiallyPaid) => true,
            (InvoiceStatus.Issued, InvoiceStatus.Paid) => true,
            (InvoiceStatus.Issued, InvoiceStatus.Canceled) => true,
            (InvoiceStatus.PartiallyPaid, InvoiceStatus.Paid) => true,
            (InvoiceStatus.PartiallyPaid, InvoiceStatus.Canceled) => true,
            _ => false,
        };

        if (!allowed)
        {
            throw new DomainException(
                $"تغییر وضعیت فاکتور از {Status} به {newStatus} مجاز نیست.",
                "BILLING_INVOICE_STATUS_INVALID");
        }

        Status = newStatus;
        MarkUpdated(userId, utcNow);
    }

    private static string NormalizeNumber(string invoiceNumber)
    {
        if (string.IsNullOrWhiteSpace(invoiceNumber))
        {
            throw new DomainException("شماره فاکتور الزامی است.", "BILLING_INVOICE_NUMBER_REQUIRED");
        }

        return invoiceNumber.Trim();
    }
}
