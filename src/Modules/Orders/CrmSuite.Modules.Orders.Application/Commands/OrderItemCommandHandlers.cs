using CrmSuite.BuildingBlocks.Application.Abstractions;
using CrmSuite.BuildingBlocks.Domain.Exceptions;
using CrmSuite.BuildingBlocks.Domain.Results;
using CrmSuite.Modules.Catalog.Application.Services;
using CrmSuite.Modules.Customers.Application.Abstractions;
using CrmSuite.Modules.Foundation.Application.Abstractions;
using CrmSuite.Modules.Orders.Application.Abstractions;
using CrmSuite.Modules.Orders.Application.Dtos;
using CrmSuite.Modules.Orders.Application.Mapping;
using CrmSuite.Modules.Orders.Domain.Entities;
using MediatR;
using FoundationCurrentUser = CrmSuite.Modules.Foundation.Application.Abstractions.ICurrentUserService;

namespace CrmSuite.Modules.Orders.Application.Commands;

public sealed class AddOrderItemCommandHandler
    : IRequestHandler<AddOrderItemCommand, Result<OrderDto>>
{
    private readonly IOrderRepository _orders;
    private readonly IOrderUnitOfWork _unitOfWork;
    private readonly ICustomerRepository _customers;
    private readonly IServicePricingService _pricing;
    private readonly IAuditScope _auditScope;
    private readonly FoundationCurrentUser _currentUser;
    private readonly IDateTimeProvider _clock;
    private readonly OrderDtoMapper _mapper;

    public AddOrderItemCommandHandler(
        IOrderRepository orders,
        IOrderUnitOfWork unitOfWork,
        ICustomerRepository customers,
        IServicePricingService pricing,
        IAuditScope auditScope,
        FoundationCurrentUser currentUser,
        IDateTimeProvider clock,
        OrderDtoMapper mapper)
    {
        _orders = orders;
        _unitOfWork = unitOfWork;
        _customers = customers;
        _pricing = pricing;
        _auditScope = auditScope;
        _currentUser = currentUser;
        _clock = clock;
        _mapper = mapper;
    }

    public async Task<Result<OrderDto>> Handle(
        AddOrderItemCommand request,
        CancellationToken cancellationToken)
    {
        var order = await _orders.GetByIdWithDetailsAsync(request.OrderId, cancellationToken);
        if (order is null)
        {
            return Result.Failure<OrderDto>("سفارش یافت نشد.", "ORDERS_ORDER_NOT_FOUND");
        }

        try
        {
            order.EnsureEditable();
        }
        catch (DomainException exception)
        {
            return Result.Failure<OrderDto>(exception.Message, exception.Code);
        }

        var pricing = await _pricing.CalculateAsync(
            new ServicePricingRequest(
                request.ServiceId,
                request.Quantity,
                (request.Addons ?? [])
                    .Select(addon => new AddonSelection(addon.AddonId, addon.Quantity))
                    .ToArray(),
                request.AttributesJson),
            cancellationToken);

        if (pricing.IsFailure)
        {
            return Result.Failure<OrderDto>(pricing.Errors, pricing.Message);
        }

        var serviceName = pricing.Value!.Preview.ServiceName;
        var item = new OrderItem(
            order.Id,
            request.ServiceId,
            serviceName,
            request.Quantity <= 0 ? 1 : request.Quantity,
            pricing.Value.UnitPriceSnapshot,
            pricing.Value.AddonsTotal,
            pricing.Value.AddonsJson,
            request.AttributesJson,
            request.BillingCycle,
            _clock.UtcNow,
            _currentUser.UserId);

        order.AddItem(item);
        order.RecalculateTotals();
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        _auditScope.EntityId = order.Id.ToString();

        var customer = await _customers.GetByIdAsync(
            order.CustomerId,
            false,
            cancellationToken);
        return Result.Success(_mapper.ToOrderDto(order, customer?.DisplayName));
    }
}

public sealed class OverrideOrderItemPriceCommandHandler
    : IRequestHandler<OverrideOrderItemPriceCommand, Result<OrderDto>>
{
    private readonly IOrderRepository _orders;
    private readonly IOrderUnitOfWork _unitOfWork;
    private readonly ICustomerRepository _customers;
    private readonly IAuditScope _auditScope;
    private readonly FoundationCurrentUser _currentUser;
    private readonly IDateTimeProvider _clock;
    private readonly OrderDtoMapper _mapper;

    public OverrideOrderItemPriceCommandHandler(
        IOrderRepository orders,
        IOrderUnitOfWork unitOfWork,
        ICustomerRepository customers,
        IAuditScope auditScope,
        FoundationCurrentUser currentUser,
        IDateTimeProvider clock,
        OrderDtoMapper mapper)
    {
        _orders = orders;
        _unitOfWork = unitOfWork;
        _customers = customers;
        _auditScope = auditScope;
        _currentUser = currentUser;
        _clock = clock;
        _mapper = mapper;
    }

    public async Task<Result<OrderDto>> Handle(
        OverrideOrderItemPriceCommand request,
        CancellationToken cancellationToken)
    {
        var order = await _orders.GetByIdWithDetailsAsync(request.OrderId, cancellationToken);
        if (order is null)
        {
            return Result.Failure<OrderDto>("سفارش یافت نشد.", "ORDERS_ORDER_NOT_FOUND");
        }

        if (string.IsNullOrWhiteSpace(request.Reason))
        {
            return Result.Failure<OrderDto>(
                "دلیل تغییر قیمت الزامی است.",
                "ORDERS_OVERRIDE_REASON_REQUIRED");
        }

        var item = order.Items.SingleOrDefault(orderItem => orderItem.Id == request.ItemId);
        if (item is null)
        {
            return Result.Failure<OrderDto>("آیتم سفارش یافت نشد.", "ORDERS_ITEM_NOT_FOUND");
        }

        try
        {
            order.EnsureEditable();
            item.OverrideUnitPrice(
                request.OverridePrice,
                request.Reason,
                _currentUser.UserId,
                _clock.UtcNow);
            order.RecalculateTotals();
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            _auditScope.EntityId = order.Id.ToString();
        }
        catch (DomainException exception)
        {
            return Result.Failure<OrderDto>(exception.Message, exception.Code);
        }

        var customer = await _customers.GetByIdAsync(
            order.CustomerId,
            false,
            cancellationToken);
        return Result.Success(_mapper.ToOrderDto(order, customer?.DisplayName));
    }
}

public sealed class UpdateOrderItemAttributesCommandHandler
    : IRequestHandler<UpdateOrderItemAttributesCommand, Result<OrderDto>>
{
    private readonly IOrderRepository _orders;
    private readonly IOrderUnitOfWork _unitOfWork;
    private readonly ICustomerRepository _customers;
    private readonly IAuditScope _auditScope;
    private readonly FoundationCurrentUser _currentUser;
    private readonly IDateTimeProvider _clock;
    private readonly OrderDtoMapper _mapper;

    public UpdateOrderItemAttributesCommandHandler(
        IOrderRepository orders,
        IOrderUnitOfWork unitOfWork,
        ICustomerRepository customers,
        IAuditScope auditScope,
        FoundationCurrentUser currentUser,
        IDateTimeProvider clock,
        OrderDtoMapper mapper)
    {
        _orders = orders;
        _unitOfWork = unitOfWork;
        _customers = customers;
        _auditScope = auditScope;
        _currentUser = currentUser;
        _clock = clock;
        _mapper = mapper;
    }

    public async Task<Result<OrderDto>> Handle(
        UpdateOrderItemAttributesCommand request,
        CancellationToken cancellationToken)
    {
        var order = await _orders.GetByIdWithDetailsAsync(request.OrderId, cancellationToken);
        if (order is null)
        {
            return Result.Failure<OrderDto>("سفارش یافت نشد.", "ORDERS_ORDER_NOT_FOUND");
        }

        var item = order.Items.SingleOrDefault(orderItem => orderItem.Id == request.ItemId);
        if (item is null)
        {
            return Result.Failure<OrderDto>("آیتم سفارش یافت نشد.", "ORDERS_ITEM_NOT_FOUND");
        }

        try
        {
            order.EnsureEditable();
            item.UpdateAttributes(request.AttributesJson, _currentUser.UserId, _clock.UtcNow);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            _auditScope.EntityId = order.Id.ToString();
        }
        catch (DomainException exception)
        {
            return Result.Failure<OrderDto>(exception.Message, exception.Code);
        }

        var customer = await _customers.GetByIdAsync(
            order.CustomerId,
            false,
            cancellationToken);
        return Result.Success(_mapper.ToOrderDto(order, customer?.DisplayName));
    }
}
