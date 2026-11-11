using CrmSuite.BuildingBlocks.Domain.Results;
using CrmSuite.Modules.Payments.Application.Dtos;
using CrmSuite.Modules.Payments.Domain.Enums;
using MediatR;

namespace CrmSuite.Modules.Payments.Application.Queries;

public sealed record GetTransactionsQuery(
    int Page = 1,
    int PageSize = 20,
    TransactionStatus? Status = null,
    Guid? CustomerId = null,
    Guid? InvoiceId = null,
    DateTime? FromUtc = null,
    DateTime? ToUtc = null) : IRequest<Result<PagedResult<TransactionListItemDto>>>;

public sealed record GetCustomerTransactionsQuery : IRequest<Result<IReadOnlyList<TransactionListItemDto>>>;
