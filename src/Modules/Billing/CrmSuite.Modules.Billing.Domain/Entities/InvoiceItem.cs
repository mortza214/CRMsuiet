using CrmSuite.BuildingBlocks.Domain.Exceptions;
using CrmSuite.Modules.Foundation.Domain.Entities;

namespace CrmSuite.Modules.Billing.Domain.Entities;

public sealed class InvoiceItem : BaseEntity
{
    private InvoiceItem()
    {
    }

    public InvoiceItem(
        Guid invoiceId,
        string itemName,
        int quantity,
        decimal unitPrice,
        DateTime createdAtUtc)
        : base(Guid.NewGuid())
    {
        if (quantity <= 0)
        {
            throw new DomainException("تعداد آیتم فاکتور باید بزرگ‌تر از صفر باشد.", "BILLING_ITEM_QUANTITY_INVALID");
        }

        if (unitPrice < 0)
        {
            throw new DomainException("قیمت واحد فاکتور نمی‌تواند منفی باشد.", "BILLING_ITEM_PRICE_INVALID");
        }

        InvoiceId = invoiceId;
        ItemName = string.IsNullOrWhiteSpace(itemName) ? "آیتم فاکتور" : itemName.Trim();
        Quantity = quantity;
        UnitPrice = unitPrice;
        TotalPrice = quantity * unitPrice;
        MarkCreated(null, createdAtUtc);
    }

    public Guid InvoiceId { get; private set; }

    public string ItemName { get; private set; } = string.Empty;

    public int Quantity { get; private set; }

    public decimal UnitPrice { get; private set; }

    public decimal TotalPrice { get; private set; }
}
