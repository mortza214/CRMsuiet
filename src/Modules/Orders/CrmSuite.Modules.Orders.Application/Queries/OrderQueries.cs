using CrmSuite.BuildingBlocks.Domain.Results;
using CrmSuite.Modules.Orders.Application.Dtos;
using CrmSuite.Modules.Orders.Domain.Enums;
using MediatR;

namespace CrmSuite.Modules.Orders.Application.Queries;

public sealed record GetOrdersQuery(
    int Page = 1,
    int PageSize = 20,
    OrderStatus? Status = null,
    Guid? CustomerId = null,
    Guid? SalesExpertId = null,
    DateTime? FromUtc = null,
    DateTime? ToUtc = null,
    string? Search = null) : IRequest<Result<PagedResult<OrderListItemDto>>>;

public sealed record GetOrderByIdQuery(Guid Id) : IRequest<Result<OrderDto>>;

public sealed record GetCustomerOrdersQuery : IRequest<Result<IReadOnlyList<OrderListItemDto>>>;

public sealed record GetCustomerServicesQuery(Guid CustomerId) : IRequest<Result<IReadOnlyList<CustomerServiceDto>>>;

public sealed record GetMyOrderByIdQuery(Guid Id) : IRequest<Result<OrderDto>>;
