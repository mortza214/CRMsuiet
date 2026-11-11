using CrmSuite.BuildingBlocks.Domain.Results;
using CrmSuite.Modules.Customers.Application.Dtos;
using MediatR;

namespace CrmSuite.Modules.Customers.Application.Queries;

public sealed record GetCustomersQuery(
    int Page = 1,
    int PageSize = 20,
    string? Search = null,
    Guid? GroupId = null,
    Guid? TagId = null,
    bool? IsReseller = null,
    bool OnlyDebtors = false,
    Guid? ParentCustomerId = null) : IRequest<Result<PagedResult<CustomerListItemDto>>>;

public sealed record GetCustomerByIdQuery(Guid Id) : IRequest<Result<CustomerDto>>;

public sealed record GetMyCustomerProfileQuery : IRequest<Result<CustomerDto>>;

public sealed record GetSubCustomersQuery : IRequest<Result<IReadOnlyList<CustomerDto>>>;

public sealed record GetCustomerGroupsQuery : IRequest<Result<IReadOnlyList<CustomerGroupDto>>>;

public sealed record GetCustomerTagsQuery : IRequest<Result<IReadOnlyList<CustomerTagDto>>>;
