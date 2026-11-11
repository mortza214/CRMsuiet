using CrmSuite.BuildingBlocks.Domain.Entities;
using CrmSuite.Modules.Orders.Domain.Enums;

namespace CrmSuite.Modules.Orders.Domain.Entities;

public sealed class OrderStatusHistory : Entity<Guid>
{
    private OrderStatusHistory()
    {
    }

    public OrderStatusHistory(
        Guid orderId,
        OrderStatus fromStatus,
        OrderStatus toStatus,
        Guid? changedByUserId,
        DateTime changedAtUtc,
        string? note)
    {
        Id = Guid.NewGuid();
        OrderId = orderId;
        FromStatus = fromStatus;
        ToStatus = toStatus;
        ChangedByUserId = changedByUserId;
        ChangedAt = DateTime.SpecifyKind(changedAtUtc, DateTimeKind.Utc);
        Note = string.IsNullOrWhiteSpace(note) ? null : note.Trim();
    }

    public Guid OrderId { get; private set; }

    public OrderStatus FromStatus { get; private set; }

    public OrderStatus ToStatus { get; private set; }

    public Guid? ChangedByUserId { get; private set; }

    public DateTime ChangedAt { get; private set; }

    public string? Note { get; private set; }
}
