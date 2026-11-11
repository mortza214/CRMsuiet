using CrmSuite.BuildingBlocks.Domain.Results;
using CrmSuite.Modules.Orders.Application.Dtos;
using CrmSuite.Modules.Orders.Domain.Enums;
using MediatR;

namespace CrmSuite.Modules.Orders.Application.Commands;

public sealed record OrderAddonInput(Guid AddonId, int Quantity);

public sealed record OrderItemInput(
    Guid? ServiceId,
    Guid? DataCenterItemId,
    Guid? AddonId,
    int Quantity,
    BillingCycle BillingCycle,
    string? AttributesJson = null,
    IReadOnlyCollection<OrderAddonInput>? Addons = null);

public sealed record CreateOrderCommand(
    Guid CustomerId,
    OrderType OrderType,
    DateTime? OrderDateUtc,
    string? Notes,
    IReadOnlyCollection<OrderItemInput> Items) : IRequest<Result<OrderDto>>;

public sealed record UpdateOrderStatusCommand(
    Guid Id,
    OrderStatus Status,
    string? Note = null) : IRequest<Result<OrderDto>>;

public sealed record CancelOrderCommand(
    Guid Id,
    string? Note = null) : IRequest<Result<OrderDto>>;

public sealed record ApplyDiscountCommand(
    Guid Id,
    decimal DiscountAmount) : IRequest<Result<OrderDto>>;
