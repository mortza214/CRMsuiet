using CrmSuite.Modules.Foundation.Application.Abstractions;
using CrmSuite.Modules.Orders.Application.Dtos;
using CrmSuite.Modules.Orders.Domain.Entities;

namespace CrmSuite.Modules.Orders.Application.Mapping;

public sealed class OrderDtoMapper
{
    private readonly IJalaliDateService _jalaliDate;

    public OrderDtoMapper(IJalaliDateService jalaliDate)
    {
        _jalaliDate = jalaliDate;
    }

    public OrderDto ToOrderDto(Order order, string? customerName)
    {
        var orderDate = AsUtc(order.OrderDate);
        var createdAt = AsUtc(order.CreatedAt);

        return new OrderDto(
            order.Id,
            order.OrderNumber,
            order.CustomerId,
            customerName,
            order.SalesExpertId,
            orderDate,
            _jalaliDate.ToJalali(orderDate),
            order.Status.ToString(),
            order.OrderType.ToString(),
            order.TotalAmount,
            order.DiscountAmount,
            order.PayableAmount,
            order.Notes,
            order.Items
                .OrderBy(item => item.CreatedAt)
                .Select(ToItemDto)
                .ToArray(),
            order.StatusHistory
                .OrderBy(history => history.ChangedAt)
                .Select(ToHistoryDto)
                .ToArray(),
            createdAt,
            _jalaliDate.ToJalali(createdAt));
    }

    public OrderItemDto ToItemDto(OrderItem item)
    {
        var createdAt = AsUtc(item.CreatedAt);

        return new OrderItemDto(
            item.Id,
            item.ServiceId,
            item.DataCenterItemId,
            item.AddonId,
            item.ItemName,
            item.Quantity,
            item.UnitPrice,
            item.TotalPrice,
            item.BillingCycle.ToString(),
            createdAt,
            _jalaliDate.ToJalali(createdAt),
            item.UnitPriceSnapshot,
            item.AddonsJson,
            item.AttributesJson,
            item.OverridePrice,
            item.OverrideReason,
            item.OverrideBy,
            item.EffectivePrice,
            item.CommissionBase);
    }

    public OrderStatusHistoryDto ToHistoryDto(OrderStatusHistory history)
    {
        var changedAt = AsUtc(history.ChangedAt);

        return new OrderStatusHistoryDto(
            history.Id,
            history.FromStatus.ToString(),
            history.ToStatus.ToString(),
            history.ChangedByUserId,
            changedAt,
            _jalaliDate.ToJalali(changedAt),
            history.Note);
    }

    private static DateTime AsUtc(DateTime value) =>
        value.Kind == DateTimeKind.Utc ? value : DateTime.SpecifyKind(value, DateTimeKind.Utc);
}
