using CrmSuite.BuildingBlocks.Domain.Results;
using CrmSuite.Modules.Customers.Application.Abstractions;
using CrmSuite.Modules.Orders.Application.Abstractions;
using CrmSuite.Modules.Orders.Application.Dtos;
using CrmSuite.Modules.Orders.Application.Mapping;
using MediatR;
using FoundationCurrentUser = CrmSuite.Modules.Foundation.Application.Abstractions.ICurrentUserService;

namespace CrmSuite.Modules.Orders.Application.Queries;

public sealed class GetOrdersQueryHandler
    : IRequestHandler<GetOrdersQuery, Result<PagedResult<OrderListItemDto>>>
{
    private readonly IOrderReadRepository _readRepository;

    public GetOrdersQueryHandler(IOrderReadRepository readRepository)
    {
        _readRepository = readRepository;
    }

    public async Task<Result<PagedResult<OrderListItemDto>>> Handle(
        GetOrdersQuery request,
        CancellationToken cancellationToken)
    {
        var page = await _readRepository.SearchAsync(
            new OrderSearchFilter(
                request.Status,
                request.CustomerId,
                request.SalesExpertId,
                request.FromUtc,
                request.ToUtc,
                request.Search,
                request.Page,
                request.PageSize),
            cancellationToken);

        return Result.Success(page);
    }
}

public sealed class GetOrderByIdQueryHandler
    : IRequestHandler<GetOrderByIdQuery, Result<OrderDto>>
{
    private readonly IOrderRepository _orders;
    private readonly ICustomerRepository _customers;
    private readonly OrderDtoMapper _mapper;

    public GetOrderByIdQueryHandler(
        IOrderRepository orders,
        ICustomerRepository customers,
        OrderDtoMapper mapper)
    {
        _orders = orders;
        _customers = customers;
        _mapper = mapper;
    }

    public async Task<Result<OrderDto>> Handle(
        GetOrderByIdQuery request,
        CancellationToken cancellationToken)
    {
        var order = await _orders.GetByIdWithDetailsAsync(request.Id, cancellationToken);
        if (order is null)
        {
            return Result.Failure<OrderDto>("سفارش یافت نشد.", "ORDERS_NOT_FOUND");
        }

        var customer = await _customers.GetByIdAsync(order.CustomerId, false, cancellationToken);
        return Result.Success(_mapper.ToOrderDto(order, customer?.DisplayName));
    }
}

public sealed class GetMyOrderByIdQueryHandler
    : IRequestHandler<GetMyOrderByIdQuery, Result<OrderDto>>
{
    private readonly IOrderRepository _orders;
    private readonly ICustomerRepository _customers;
    private readonly OrderDtoMapper _mapper;
    private readonly FoundationCurrentUser _currentUser;

    public GetMyOrderByIdQueryHandler(
        IOrderRepository orders,
        ICustomerRepository customers,
        OrderDtoMapper mapper,
        FoundationCurrentUser currentUser)
    {
        _orders = orders;
        _customers = customers;
        _mapper = mapper;
        _currentUser = currentUser;
    }

    public async Task<Result<OrderDto>> Handle(
        GetMyOrderByIdQuery request,
        CancellationToken cancellationToken)
    {
        if (_currentUser.UserId is null)
        {
            return Result.Failure<OrderDto>(
                "کد کاربر جاری قابل تشخیص نیست.",
                "ORDERS_CURRENT_USER_MISSING");
        }

        var customer = await _customers.GetByUserIdAsync(
            _currentUser.UserId.Value,
            includeDetails: false,
            cancellationToken);

        if (customer is null)
        {
            return Result.Failure<OrderDto>(
                "پروفایل مشتری یافت نشد.",
                "ORDERS_CUSTOMER_NOT_FOUND");
        }

        var order = await _orders.GetByIdWithDetailsAsync(request.Id, cancellationToken);
        if (order is null || order.CustomerId != customer.Id)
        {
            return Result.Failure<OrderDto>("سفارش یافت نشد.", "ORDERS_NOT_FOUND");
        }

        return Result.Success(_mapper.ToOrderDto(order, customer.DisplayName));
    }
}

public sealed class GetCustomerOrdersQueryHandler
    : IRequestHandler<GetCustomerOrdersQuery, Result<IReadOnlyList<OrderListItemDto>>>
{
    private readonly ICustomerRepository _customers;
    private readonly IOrderReadRepository _readRepository;
    private readonly FoundationCurrentUser _currentUser;

    public GetCustomerOrdersQueryHandler(
        ICustomerRepository customers,
        IOrderReadRepository readRepository,
        FoundationCurrentUser currentUser)
    {
        _customers = customers;
        _readRepository = readRepository;
        _currentUser = currentUser;
    }

    public async Task<Result<IReadOnlyList<OrderListItemDto>>> Handle(
        GetCustomerOrdersQuery request,
        CancellationToken cancellationToken)
    {
        if (_currentUser.UserId is null)
        {
            return Result.Failure<IReadOnlyList<OrderListItemDto>>(
                "کاربر جاری قابل تشخیص نیست.",
                "ORDERS_CURRENT_USER_MISSING");
        }

        var customer = await _customers.GetByUserIdAsync(
            _currentUser.UserId.Value,
            includeDetails: false,
            cancellationToken);

        if (customer is null)
        {
            return Result.Failure<IReadOnlyList<OrderListItemDto>>(
                "پروفایل مشتری یافت نشد.",
                "ORDERS_CUSTOMER_NOT_FOUND");
        }

        var orders = await _readRepository.GetCustomerOrdersAsync(customer.Id, cancellationToken);
        return Result.Success(orders);
    }
}
