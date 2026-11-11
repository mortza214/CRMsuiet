using CrmSuite.BuildingBlocks.Domain.Exceptions;
using CrmSuite.Modules.Foundation.Domain.Entities;
using CrmSuite.Modules.Orders.Domain.Enums;

namespace CrmSuite.Modules.Orders.Domain.Entities;

public sealed class Order : BaseEntity
{
    private Order()
    {
    }

    public Order(
        string orderNumber,
        Guid customerId,
        Guid salesExpertId,
        DateTime orderDateUtc,
        OrderType orderType,
        string? notes,
        Guid? createdByUserId,
        DateTime createdAtUtc)
        : base(Guid.NewGuid())
    {
        OrderNumber = NormalizeNumber(orderNumber);
        CustomerId = customerId;
        SalesExpertId = salesExpertId;
        OrderDate = DateTime.SpecifyKind(orderDateUtc, DateTimeKind.Utc);
        OrderType = orderType;
        Notes = string.IsNullOrWhiteSpace(notes) ? null : notes.Trim();
        Status = OrderStatus.Draft;
        MarkCreated(createdByUserId, createdAtUtc);
    }

    public string OrderNumber { get; private set; } = string.Empty;

    public Guid CustomerId { get; private set; }

    public Guid SalesExpertId { get; private set; }

    public DateTime OrderDate { get; private set; }

    public OrderStatus Status { get; private set; }

    public decimal TotalAmount { get; private set; }

    public decimal DiscountAmount { get; private set; }

    public decimal PayableAmount { get; private set; }

    public string? Notes { get; private set; }

    public Guid? WorkflowInstanceId { get; private set; }

    public DateTime? ActivatedAt { get; private set; }

    public Guid? ActivatedBy { get; private set; }

    public OrderType OrderType { get; private set; }

    public ICollection<OrderItem> Items { get; private set; } = new List<OrderItem>();

    public ICollection<OrderStatusHistory> StatusHistory { get; private set; } = new List<OrderStatusHistory>();

    public void AddItem(OrderItem item)
    {
        if (item.OrderId != Id)
        {
            throw new DomainException("آیتم به سفارش جاری تعلق ندارد.", "ORDERS_ITEM_ORDER_MISMATCH");
        }

        Items.Add(item);
        TotalAmount += item.TotalPrice;
        PayableAmount = TotalAmount - DiscountAmount;
    }

    public void RecalculateTotals()
    {
        TotalAmount = Items.Sum(item => item.TotalPrice);
        PayableAmount = TotalAmount - DiscountAmount;
    }

    public void EnsureEditable()
    {
        if (Status is not (OrderStatus.Draft or OrderStatus.PendingPayment))
        {
            throw new DomainException(
                "آیتم‌های سفارش فقط در وضعیت پیش‌نویس یا در انتظار پرداخت قابل ویرایش هستند.",
                "ORDERS_NOT_EDITABLE");
        }
    }

    public void ApplyDiscount(decimal discountAmount, Guid? userId, DateTime utcNow)
    {
        if (Status is not (OrderStatus.Draft or OrderStatus.PendingPayment))
        {
            throw new DomainException(
                "تخفیف فقط در وضعیت پیش‌نویس یا در انتظار پرداخت قابل اعمال است.",
                "ORDERS_DISCOUNT_STATUS_INVALID");
        }

        if (discountAmount < 0 || discountAmount > TotalAmount)
        {
            throw new DomainException("مبلغ تخفیف معتبر نیست.", "ORDERS_DISCOUNT_INVALID");
        }

        DiscountAmount = discountAmount;
        PayableAmount = TotalAmount - DiscountAmount;
        MarkUpdated(userId, utcNow);
    }

    public void ChangeStatus(
        OrderStatus newStatus,
        Guid? changedByUserId,
        DateTime utcNow,
        string? note)
    {
        if (newStatus == Status)
        {
            return;
        }

        if (!CanTransition(Status, newStatus))
        {
            throw new DomainException(
                $"تغییر وضعیت از {Status} به {newStatus} مجاز نیست.",
                "ORDERS_STATUS_TRANSITION_INVALID");
        }

        var history = new OrderStatusHistory(
            Id,
            Status,
            newStatus,
            changedByUserId,
            utcNow,
            note);

        Status = newStatus;
        StatusHistory.Add(history);
        MarkUpdated(changedByUserId, utcNow);
    }

    public void Cancel(Guid? userId, DateTime utcNow, string? note = null)
    {
        ChangeStatus(OrderStatus.Canceled, userId, utcNow, note);
    }

    public void AttachWorkflowInstance(Guid instanceId, DateTime utcNow, Guid? userId = null)
    {
        if (instanceId == Guid.Empty)
        {
            throw new DomainException("شناسه گردش کار معتبر نیست.", "ORDERS_WORKFLOW_INSTANCE_INVALID");
        }

        WorkflowInstanceId = instanceId;
        MarkUpdated(userId, utcNow);
    }

    public void Activate(Guid? userId, DateTime utcNow)
    {
        if (Status == OrderStatus.Completed
            && ActivatedAt.HasValue)
        {
            return;
        }

        if (Status is OrderStatus.Canceled or OrderStatus.Draft)
        {
            throw new DomainException(
                "سفارش در وضعیت فعلی قابل فعال‌سازی نیست.",
                "ORDERS_ACTIVATION_STATUS_INVALID");
        }

        var previousStatus = Status;
        ActivatedAt = DateTime.SpecifyKind(utcNow, DateTimeKind.Utc);
        ActivatedBy = userId;
        Status = OrderStatus.Completed;
        StatusHistory.Add(new OrderStatusHistory(
            Id,
            previousStatus,
            OrderStatus.Completed,
            userId,
            utcNow,
            "فعال‌سازی سرویس"));
        MarkUpdated(userId, utcNow);
    }

    public static bool CanTransition(OrderStatus from, OrderStatus to) =>
        (from, to) switch
        {
            (OrderStatus.Draft, OrderStatus.Pending) => true,
            (OrderStatus.Draft, OrderStatus.PendingPayment) => true,
            (OrderStatus.Draft, OrderStatus.Canceled) => true,
            (OrderStatus.Pending, OrderStatus.PendingPayment) => true,
            (OrderStatus.Pending, OrderStatus.Provisioning) => true,
            (OrderStatus.Pending, OrderStatus.Canceled) => true,
            (OrderStatus.PendingPayment, OrderStatus.Provisioning) => true,
            (OrderStatus.PendingPayment, OrderStatus.Paid) => true,
            (OrderStatus.PendingPayment, OrderStatus.Canceled) => true,
            (OrderStatus.Provisioning, OrderStatus.Canceled) => true,
            (OrderStatus.Paid, OrderStatus.Provisioning) => true,
            (OrderStatus.Paid, OrderStatus.Canceled) => true,
            (OrderStatus.Provisioning, OrderStatus.Completed) => true,
            _ => false,
        };

    private static string NormalizeNumber(string orderNumber)
    {
        if (string.IsNullOrWhiteSpace(orderNumber))
        {
            throw new DomainException("شماره سفارش الزامی است.", "ORDERS_NUMBER_REQUIRED");
        }

        return orderNumber.Trim();
    }
}
