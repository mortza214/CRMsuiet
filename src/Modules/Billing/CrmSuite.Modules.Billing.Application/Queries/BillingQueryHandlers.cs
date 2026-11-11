using CrmSuite.BuildingBlocks.Domain.Results;
using CrmSuite.Modules.Billing.Application.Abstractions;
using CrmSuite.Modules.Billing.Application.Dtos;
using CrmSuite.Modules.Billing.Application.Mapping;
using CrmSuite.Modules.Customers.Application.Abstractions;
using CrmSuite.Modules.Orders.Application.Abstractions;
using MediatR;
using FoundationCurrentUser = CrmSuite.Modules.Foundation.Application.Abstractions.ICurrentUserService;

namespace CrmSuite.Modules.Billing.Application.Queries;

public sealed class GetInvoicesQueryHandler
    : IRequestHandler<GetInvoicesQuery, Result<PagedResult<InvoiceListItemDto>>>
{
    private readonly IInvoiceReadRepository _readRepository;

    public GetInvoicesQueryHandler(IInvoiceReadRepository readRepository)
    {
        _readRepository = readRepository;
    }

    public async Task<Result<PagedResult<InvoiceListItemDto>>> Handle(
        GetInvoicesQuery request,
        CancellationToken cancellationToken)
    {
        var page = await _readRepository.SearchAsync(
            new InvoiceSearchFilter(
                request.Status,
                request.CustomerId,
                request.FromUtc,
                request.ToUtc,
                request.Search,
                request.Page,
                request.PageSize),
            cancellationToken);

        return Result.Success(page);
    }
}

public sealed class GetInvoiceByIdQueryHandler
    : IRequestHandler<GetInvoiceByIdQuery, Result<InvoiceDto>>
{
    private readonly IInvoiceRepository _invoices;
    private readonly ICustomerRepository _customers;
    private readonly IOrderRepository _orders;
    private readonly BillingDtoMapper _mapper;

    public GetInvoiceByIdQueryHandler(
        IInvoiceRepository invoices,
        ICustomerRepository customers,
        IOrderRepository orders,
        BillingDtoMapper mapper)
    {
        _invoices = invoices;
        _customers = customers;
        _orders = orders;
        _mapper = mapper;
    }

    public async Task<Result<InvoiceDto>> Handle(
        GetInvoiceByIdQuery request,
        CancellationToken cancellationToken)
    {
        var invoice = await _invoices.GetByIdWithItemsAsync(request.Id, cancellationToken);
        if (invoice is null)
        {
            return Result.Failure<InvoiceDto>("فاکتور یافت نشد.", "BILLING_INVOICE_NOT_FOUND");
        }

        var customer = await _customers.GetByIdAsync(invoice.CustomerId, false, cancellationToken);
        var order = await _orders.GetByIdWithDetailsAsync(invoice.OrderId, cancellationToken);
        return Result.Success(_mapper.ToInvoiceDto(invoice, customer?.DisplayName, order?.OrderNumber));
    }
}

public sealed class GetCustomerInvoicesQueryHandler
    : IRequestHandler<GetCustomerInvoicesQuery, Result<IReadOnlyList<InvoiceListItemDto>>>
{
    private readonly ICustomerRepository _customers;
    private readonly IInvoiceReadRepository _readRepository;
    private readonly FoundationCurrentUser _currentUser;

    public GetCustomerInvoicesQueryHandler(
        ICustomerRepository customers,
        IInvoiceReadRepository readRepository,
        FoundationCurrentUser currentUser)
    {
        _customers = customers;
        _readRepository = readRepository;
        _currentUser = currentUser;
    }

    public async Task<Result<IReadOnlyList<InvoiceListItemDto>>> Handle(
        GetCustomerInvoicesQuery request,
        CancellationToken cancellationToken)
    {
        if (_currentUser.UserId is null)
        {
            return Result.Failure<IReadOnlyList<InvoiceListItemDto>>(
                "کاربر جاری قابل تشخیص نیست.",
                "BILLING_CURRENT_USER_MISSING");
        }

        var customer = await _customers.GetByUserIdAsync(
            _currentUser.UserId.Value,
            false,
            cancellationToken);

        if (customer is null)
        {
            return Result.Failure<IReadOnlyList<InvoiceListItemDto>>(
                "پروفایل مشتری یافت نشد.",
                "BILLING_CUSTOMER_NOT_FOUND");
        }

        var invoices = await _readRepository.GetCustomerInvoicesAsync(customer.Id, cancellationToken);
        return Result.Success(invoices);
    }
}

public sealed class GetCustomerInvoiceByIdQueryHandler
    : IRequestHandler<GetCustomerInvoiceByIdQuery, Result<InvoiceDto>>
{
    private readonly IInvoiceRepository _invoices;
    private readonly ICustomerRepository _customers;
    private readonly IOrderRepository _orders;
    private readonly FoundationCurrentUser _currentUser;
    private readonly BillingDtoMapper _mapper;

    public GetCustomerInvoiceByIdQueryHandler(
        IInvoiceRepository invoices,
        ICustomerRepository customers,
        IOrderRepository orders,
        FoundationCurrentUser currentUser,
        BillingDtoMapper mapper)
    {
        _invoices = invoices;
        _customers = customers;
        _orders = orders;
        _currentUser = currentUser;
        _mapper = mapper;
    }

    public async Task<Result<InvoiceDto>> Handle(
        GetCustomerInvoiceByIdQuery request,
        CancellationToken cancellationToken)
    {
        if (_currentUser.UserId is null)
        {
            return Result.Failure<InvoiceDto>("کاربر جاری قابل تشخیص نیست.", "BILLING_CURRENT_USER_MISSING");
        }

        var customer = await _customers.GetByUserIdAsync(
            _currentUser.UserId.Value,
            false,
            cancellationToken);

        if (customer is null)
        {
            return Result.Failure<InvoiceDto>("پروفایل مشتری یافت نشد.", "BILLING_CUSTOMER_NOT_FOUND");
        }

        var invoice = await _invoices.GetByIdWithItemsAsync(request.Id, cancellationToken);
        if (invoice is null || invoice.CustomerId != customer.Id)
        {
            return Result.Failure<InvoiceDto>("فاکتور یافت نشد.", "BILLING_INVOICE_NOT_FOUND");
        }

        var order = await _orders.GetByIdWithDetailsAsync(invoice.OrderId, cancellationToken);
        return Result.Success(_mapper.ToInvoiceDto(invoice, customer.DisplayName, order?.OrderNumber));
    }
}

public sealed class GetContractByIdQueryHandler
    : IRequestHandler<GetContractByIdQuery, Result<ContractDto>>
{
    private readonly IContractRepository _contracts;
    private readonly ICustomerRepository _customers;
    private readonly BillingDtoMapper _mapper;

    public GetContractByIdQueryHandler(
        IContractRepository contracts,
        ICustomerRepository customers,
        BillingDtoMapper mapper)
    {
        _contracts = contracts;
        _customers = customers;
        _mapper = mapper;
    }

    public async Task<Result<ContractDto>> Handle(
        GetContractByIdQuery request,
        CancellationToken cancellationToken)
    {
        var contract = await _contracts.GetByIdAsync(request.Id, cancellationToken);
        if (contract is null)
        {
            return Result.Failure<ContractDto>("قرارداد یافت نشد.", "BILLING_CONTRACT_NOT_FOUND");
        }

        var customer = await _customers.GetByIdAsync(contract.CustomerId, false, cancellationToken);
        return Result.Success(_mapper.ToContractDto(contract, customer?.DisplayName));
    }
}
