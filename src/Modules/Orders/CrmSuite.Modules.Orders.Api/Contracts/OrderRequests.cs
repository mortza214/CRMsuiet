using CrmSuite.Modules.Orders.Domain.Enums;

namespace CrmSuite.Modules.Orders.Api.Contracts;

public sealed class CreateOrderItemRequest
{
    public Guid? ServiceId { get; init; }

    public Guid? DataCenterItemId { get; init; }

    public Guid? AddonId { get; init; }

    public int Quantity { get; init; }

    public BillingCycle BillingCycle { get; init; }

    public string? AttributesJson { get; init; }

    public List<OrderAddonRequest> Addons { get; init; } = [];
}

public sealed class AddOrderItemRequest
{
    public Guid ServiceId { get; init; }

    public int Quantity { get; init; }

    public string? AttributesJson { get; init; }

    public BillingCycle BillingCycle { get; init; } = BillingCycle.Monthly;

    public List<OrderAddonRequest> Addons { get; init; } = [];
}

public sealed class OrderAddonRequest
{
    public Guid AddonId { get; init; }

    public int Quantity { get; init; } = 1;
}

public sealed class UpdateOrderItemAttributesRequest
{
    public string? AttributesJson { get; init; }
}

public sealed class OverrideOrderItemPriceRequest
{
    public decimal OverridePrice { get; init; }

    public string Reason { get; init; } = string.Empty;
}

public sealed class CreateOrderRequest
{
    public Guid CustomerId { get; init; }

    public OrderType OrderType { get; init; }

    public DateTime? OrderDateUtc { get; init; }

    public string? Notes { get; init; }

    public List<CreateOrderItemRequest> Items { get; init; } = [];
}

public sealed class UpdateOrderStatusRequest
{
    public OrderStatus Status { get; init; }

    public string? Note { get; init; }
}

public sealed class ApplyDiscountRequest
{
    public decimal DiscountAmount { get; init; }
}

public sealed class CancelOrderRequest
{
    public string? Note { get; init; }
}
