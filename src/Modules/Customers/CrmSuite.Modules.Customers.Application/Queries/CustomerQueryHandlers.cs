using CrmSuite.BuildingBlocks.Domain.Results;
using CrmSuite.Modules.Customers.Application.Abstractions;
using CrmSuite.Modules.Customers.Application.Dtos;
using CrmSuite.Modules.Customers.Application.Mapping;
using MediatR;
using FoundationCurrentUser = CrmSuite.Modules.Foundation.Application.Abstractions.ICurrentUserService;

namespace CrmSuite.Modules.Customers.Application.Queries;

public sealed class GetCustomersQueryHandler
    : IRequestHandler<GetCustomersQuery, Result<PagedResult<CustomerListItemDto>>>
{
    private readonly ICustomerReadRepository _readRepository;

    public GetCustomersQueryHandler(ICustomerReadRepository readRepository)
    {
        _readRepository = readRepository;
    }

    public async Task<Result<PagedResult<CustomerListItemDto>>> Handle(
        GetCustomersQuery request,
        CancellationToken cancellationToken)
    {
        var page = await _readRepository.SearchAsync(
            new CustomerSearchFilter(
                request.Search,
                request.GroupId,
                request.TagId,
                request.IsReseller,
                request.OnlyDebtors,
                request.ParentCustomerId,
                request.Page,
                request.PageSize),
            cancellationToken);

        return Result.Success(page);
    }
}

public sealed class GetCustomerByIdQueryHandler
    : IRequestHandler<GetCustomerByIdQuery, Result<CustomerDto>>
{
    private readonly ICustomerRepository _customers;
    private readonly CustomerDtoMapper _mapper;

    public GetCustomerByIdQueryHandler(ICustomerRepository customers, CustomerDtoMapper mapper)
    {
        _customers = customers;
        _mapper = mapper;
    }

    public async Task<Result<CustomerDto>> Handle(
        GetCustomerByIdQuery request,
        CancellationToken cancellationToken)
    {
        var customer = await _customers.GetByIdAsync(request.Id, includeDetails: true, cancellationToken);

        return customer is null
            ? Result.Failure<CustomerDto>("مشتری یافت نشد.", "CUSTOMERS_NOT_FOUND")
            : Result.Success(_mapper.ToCustomerDto(customer));
    }
}

public sealed class GetMyCustomerProfileQueryHandler
    : IRequestHandler<GetMyCustomerProfileQuery, Result<CustomerDto>>
{
    private readonly ICustomerRepository _customers;
    private readonly FoundationCurrentUser _currentUser;
    private readonly CustomerDtoMapper _mapper;

    public GetMyCustomerProfileQueryHandler(
        ICustomerRepository customers,
        FoundationCurrentUser currentUser,
        CustomerDtoMapper mapper)
    {
        _customers = customers;
        _currentUser = currentUser;
        _mapper = mapper;
    }

    public async Task<Result<CustomerDto>> Handle(
        GetMyCustomerProfileQuery request,
        CancellationToken cancellationToken)
    {
        if (_currentUser.UserId is null)
        {
            return Result.Failure<CustomerDto>("کاربر جاری قابل تشخیص نیست.", "CUSTOMERS_CURRENT_USER_MISSING");
        }

        var customer = await _customers.GetByUserIdAsync(
            _currentUser.UserId.Value,
            includeDetails: true,
            cancellationToken);

        return customer is null
            ? Result.Failure<CustomerDto>("پروفایل مشتری یافت نشد.", "CUSTOMERS_NOT_FOUND")
            : Result.Success(_mapper.ToCustomerDto(customer));
    }
}

public sealed class GetSubCustomersQueryHandler
    : IRequestHandler<GetSubCustomersQuery, Result<IReadOnlyList<CustomerDto>>>
{
    private readonly ICustomerRepository _customers;
    private readonly FoundationCurrentUser _currentUser;
    private readonly CustomerDtoMapper _mapper;

    public GetSubCustomersQueryHandler(
        ICustomerRepository customers,
        FoundationCurrentUser currentUser,
        CustomerDtoMapper mapper)
    {
        _customers = customers;
        _currentUser = currentUser;
        _mapper = mapper;
    }

    public async Task<Result<IReadOnlyList<CustomerDto>>> Handle(
        GetSubCustomersQuery request,
        CancellationToken cancellationToken)
    {
        if (_currentUser.UserId is null)
        {
            return Result.Failure<IReadOnlyList<CustomerDto>>(
                "کاربر جاری قابل تشخیص نیست.",
                "CUSTOMERS_CURRENT_USER_MISSING");
        }

        var reseller = await _customers.GetByUserIdAsync(
            _currentUser.UserId.Value,
            includeDetails: false,
            cancellationToken);

        if (reseller is null || !reseller.IsReseller)
        {
            return Result.Failure<IReadOnlyList<CustomerDto>>(
                "این کاربر ریسلر نیست.",
                "CUSTOMERS_NOT_RESELLER");
        }

        var subCustomers = await _customers.GetSubCustomersAsync(reseller.Id, cancellationToken);
        return Result.Success<IReadOnlyList<CustomerDto>>(
            subCustomers.Select(_mapper.ToCustomerDto).ToArray());
    }
}

public sealed class GetCustomerGroupsQueryHandler
    : IRequestHandler<GetCustomerGroupsQuery, Result<IReadOnlyList<CustomerGroupDto>>>
{
    private readonly ICustomerGroupRepository _groups;
    private readonly CustomerDtoMapper _mapper;

    public GetCustomerGroupsQueryHandler(ICustomerGroupRepository groups, CustomerDtoMapper mapper)
    {
        _groups = groups;
        _mapper = mapper;
    }

    public async Task<Result<IReadOnlyList<CustomerGroupDto>>> Handle(
        GetCustomerGroupsQuery request,
        CancellationToken cancellationToken)
    {
        var groups = await _groups.GetAllAsync(cancellationToken);
        return Result.Success<IReadOnlyList<CustomerGroupDto>>(
            groups.Select(_mapper.ToGroupDto).ToArray());
    }
}

public sealed class GetCustomerTagsQueryHandler
    : IRequestHandler<GetCustomerTagsQuery, Result<IReadOnlyList<CustomerTagDto>>>
{
    private readonly ICustomerTagRepository _tags;
    private readonly CustomerDtoMapper _mapper;

    public GetCustomerTagsQueryHandler(ICustomerTagRepository tags, CustomerDtoMapper mapper)
    {
        _tags = tags;
        _mapper = mapper;
    }

    public async Task<Result<IReadOnlyList<CustomerTagDto>>> Handle(
        GetCustomerTagsQuery request,
        CancellationToken cancellationToken)
    {
        var tags = await _tags.GetAllAsync(cancellationToken);
        return Result.Success<IReadOnlyList<CustomerTagDto>>(
            tags.Select(_mapper.ToTagDto).ToArray());
    }
}
