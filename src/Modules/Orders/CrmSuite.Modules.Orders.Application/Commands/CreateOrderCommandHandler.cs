using CrmSuite.BuildingBlocks.Application.Abstractions;
using CrmSuite.BuildingBlocks.Domain.Exceptions;
using CrmSuite.BuildingBlocks.Domain.Results;
using CrmSuite.Modules.Catalog.Application.Abstractions;
using CrmSuite.Modules.Catalog.Application.Queries;
using CrmSuite.Modules.Catalog.Application.Services;
using CrmSuite.Modules.Customers.Application.Abstractions;
using CrmSuite.Modules.Foundation.Application.Abstractions;
using CrmSuite.Modules.Orders.Application.Abstractions;
using CrmSuite.Modules.Orders.Application.Dtos;
using CrmSuite.Modules.Orders.Application.Mapping;
using CrmSuite.Modules.Orders.Application.Notifications;
using CrmSuite.Modules.Orders.Domain.Entities;
using MediatR;
using FoundationCurrentUser = CrmSuite.Modules.Foundation.Application.Abstractions.ICurrentUserService;

namespace CrmSuite.Modules.Orders.Application.Commands;

public sealed class CreateOrderCommandHandler : IRequestHandler<CreateOrderCommand, Result<OrderDto>>
{
    private readonly IOrderRepository _orders;
    private readonly IOrderUnitOfWork _unitOfWork;
    private readonly IMediator _mediator;
    private readonly ICustomerRepository _customers;
    private readonly IServiceRepository _services;
    private readonly IAddonRepository _addons;
    private readonly IDataCenterItemRepository _dataCenterItems;
    private readonly ISequenceService _sequences;
    private readonly IAuditScope _auditScope;
    private readonly FoundationCurrentUser _currentUser;
    private readonly IDateTimeProvider _clock;
    private readonly OrderDtoMapper _mapper;
    private readonly IServicePricingService? _pricing;

    public CreateOrderCommandHandler(
        IOrderRepository orders,
        IOrderUnitOfWork unitOfWork,
        IMediator mediator,
        ICustomerRepository customers,
        IServiceRepository services,
        IAddonRepository addons,
        IDataCenterItemRepository dataCenterItems,
        ISequenceService sequences,
        IAuditScope auditScope,
        FoundationCurrentUser currentUser,
        IDateTimeProvider clock,
        OrderDtoMapper mapper,
        IServicePricingService? pricing = null)
    {
        _orders = orders;
        _unitOfWork = unitOfWork;
        _mediator = mediator;
        _customers = customers;
        _services = services;
        _addons = addons;
        _dataCenterItems = dataCenterItems;
        _sequences = sequences;
        _auditScope = auditScope;
        _currentUser = currentUser;
        _clock = clock;
        _mapper = mapper;
        _pricing = pricing;
    }

    public async Task<Result<OrderDto>> Handle(
        CreateOrderCommand request,
        CancellationToken cancellationToken)
    {
        if (_currentUser.UserId is null)
        {
            return Result.Failure<OrderDto>(
                "کارشناس فروش جاری قابل تشخیص نیست.",
                "ORDERS_SALES_EXPERT_REQUIRED");
        }

        var customer = await _customers.GetByIdAsync(request.CustomerId, includeDetails: false, cancellationToken);
        if (customer is null)
        {
            return Result.Failure<OrderDto>("مشتری یافت نشد.", "ORDERS_CUSTOMER_NOT_FOUND");
        }

        if (request.Items.Count == 0)
        {
            return Result.Failure<OrderDto>("سفارش باید حداقل یک آیتم داشته باشد.", "ORDERS_ITEMS_REQUIRED");
        }

        var now = _clock.UtcNow;
        var orderDate = DateTime.SpecifyKind(request.OrderDateUtc ?? now, DateTimeKind.Utc);
        string orderNumber;

        try
        {
            orderNumber = await _sequences.GetNextValueAsync("OrderNumber", cancellationToken);
        }
        catch (DomainException exception)
        {
            return Result.Failure<OrderDto>(exception.Message, exception.Code);
        }

        var order = new Order(
            orderNumber,
            request.CustomerId,
            _currentUser.UserId.Value,
            orderDate,
            request.OrderType,
            request.Notes,
            _currentUser.UserId,
            now);

        foreach (var input in request.Items)
        {
            var resolved = await ResolveItemAsync(order.Id, input, orderDate, now, cancellationToken);
            if (resolved.IsFailure)
            {
                return Result.Failure<OrderDto>(
                    resolved.Errors,
                    resolved.Message ?? "آیتم سفارش معتبر نیست.");
            }

            order.AddItem(resolved.Value!);
        }

        _orders.Add(order);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        _auditScope.EntityId = order.Id.ToString();

        await _mediator.Publish(new OrderCreatedNotification(order.Id), cancellationToken);

        var created = await _orders.GetByIdWithDetailsAsync(order.Id, cancellationToken);
        return Result.Success(_mapper.ToOrderDto(created ?? order, customer.DisplayName));
    }

    private async Task<Result<OrderItem>> ResolveItemAsync(
        Guid orderId,
        OrderItemInput input,
        DateTime orderDate,
        DateTime now,
        CancellationToken cancellationToken)
    {
        var sourceCount = new[] { input.ServiceId, input.DataCenterItemId, input.AddonId }
            .Count(id => id.HasValue);

        if (sourceCount != 1)
        {
            return Result.Failure<OrderItem>(
                "هر آیتم باید دقیقاً یکی از سرویس، آیتم دیتاسنتر یا آیتم جانبی باشد.",
                "ORDERS_ITEM_SOURCE_INVALID");
        }

        if (input.ServiceId.HasValue)
        {
            var service = await _services.GetByIdAsync(input.ServiceId.Value, cancellationToken);
            if (service is null || !service.IsActive)
            {
                return Result.Failure<OrderItem>("سرویس انتخاب‌شده یافت نشد یا فعال نیست.", "ORDERS_SERVICE_NOT_FOUND");
            }

            if (_pricing is not null
                && (input.AttributesJson is not null || input.Addons?.Count > 0))
            {
                var calculated = await _pricing.CalculateAsync(
                    new ServicePricingRequest(
                        service.Id,
                        input.Quantity,
                        (input.Addons ?? [])
                            .Select(addon => new AddonSelection(addon.AddonId, addon.Quantity))
                            .ToArray(),
                        input.AttributesJson),
                    cancellationToken);
                if (calculated.IsFailure)
                {
                    return Result.Failure<OrderItem>(
                        calculated.Errors,
                        calculated.Message);
                }

                return Result.Success(new OrderItem(
                    orderId,
                    service.Id,
                    service.Name,
                    input.Quantity <= 0 ? 1 : input.Quantity,
                    calculated.Value!.UnitPriceSnapshot,
                    calculated.Value.AddonsTotal,
                    calculated.Value.AddonsJson,
                    input.AttributesJson,
                    input.BillingCycle,
                    now,
                    _currentUser.UserId));
            }

            var priceResult = await _mediator.Send(
                new GetEffectivePriceQuery(service.Id, orderDate),
                cancellationToken);

            if (priceResult.IsFailure || priceResult.Value is null)
            {
                return Result.Failure<OrderItem>(
                    priceResult.Errors,
                    priceResult.Message ?? "برای سرویس در تاریخ سفارش قیمت معتبری وجود ندارد.");
            }

            return CreateItem(
                orderId,
                service.Id,
                null,
                null,
                service.Name,
                input,
                priceResult.Value.Price,
                now);
        }

        if (input.AddonId.HasValue)
        {
            var addon = await _addons.GetByIdAsync(input.AddonId.Value, cancellationToken);
            if (addon is null)
            {
                return Result.Failure<OrderItem>("آیتم جانبی یافت نشد.", "ORDERS_ADDON_NOT_FOUND");
            }

            return CreateItem(orderId, null, null, addon.Id, addon.Name, input, addon.Price, now);
        }

        var item = await _dataCenterItems.GetByIdAsync(input.DataCenterItemId!.Value, cancellationToken);
        if (item is null)
        {
            return Result.Failure<OrderItem>("آیتم دیتاسنتر یافت نشد.", "ORDERS_DC_ITEM_NOT_FOUND");
        }

        return CreateItem(orderId, null, item.Id, null, item.Name, input, item.Price, now);
    }

    private static Result<OrderItem> CreateItem(
        Guid orderId,
        Guid? serviceId,
        Guid? dataCenterItemId,
        Guid? addonId,
        string itemName,
        OrderItemInput input,
        decimal unitPrice,
        DateTime now)
    {
        return Result.Success(new OrderItem(
            orderId,
            serviceId,
            dataCenterItemId,
            addonId,
            itemName,
            input.Quantity,
            unitPrice,
            input.BillingCycle,
            now));
    }
}
