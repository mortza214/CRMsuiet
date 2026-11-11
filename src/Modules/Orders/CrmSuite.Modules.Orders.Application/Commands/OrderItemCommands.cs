using CrmSuite.BuildingBlocks.Domain.Results;
using CrmSuite.Modules.Orders.Application.Dtos;
using CrmSuite.Modules.Orders.Domain.Enums;
using MediatR;

namespace CrmSuite.Modules.Orders.Application.Commands;

public sealed record AddOrderItemCommand(
    Guid OrderId,
    Guid ServiceId,
    int Quantity,
    BillingCycle BillingCycle,
    string? AttributesJson = null,
    IReadOnlyCollection<OrderAddonInput>? Addons = null) : IRequest<Result<OrderDto>>;

public sealed record UpdateOrderItemAttributesCommand(
    Guid OrderId,
    Guid ItemId,
    string? AttributesJson) : IRequest<Result<OrderDto>>;

public sealed record OverrideOrderItemPriceCommand(
    Guid OrderId,
    Guid ItemId,
    decimal OverridePrice,
    string Reason) : IRequest<Result<OrderDto>>;
