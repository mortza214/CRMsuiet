using CrmSuite.BuildingBlocks.Domain.Results;
using CrmSuite.Modules.Billing.Application.Dtos;
using CrmSuite.Modules.Billing.Domain.Enums;
using MediatR;

namespace CrmSuite.Modules.Billing.Application.Queries;

public sealed record GetInvoicesQuery(
    int Page = 1,
    int PageSize = 20,
    InvoiceStatus? Status = null,
    Guid? CustomerId = null,
    DateTime? FromUtc = null,
    DateTime? ToUtc = null,
    string? Search = null) : IRequest<Result<PagedResult<InvoiceListItemDto>>>;

public sealed record GetInvoiceByIdQuery(Guid Id) : IRequest<Result<InvoiceDto>>;

public sealed record GetCustomerInvoicesQuery : IRequest<Result<IReadOnlyList<InvoiceListItemDto>>>;

public sealed record GetCustomerInvoiceByIdQuery(Guid Id) : IRequest<Result<InvoiceDto>>;

public sealed record GetContractByIdQuery(Guid Id) : IRequest<Result<ContractDto>>;
