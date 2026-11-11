using CrmSuite.Modules.Payments.Domain.Enums;

namespace CrmSuite.Modules.Payments.Application.Queries;

public sealed record TransactionSearchFilter(
    TransactionStatus? Status,
    Guid? CustomerId,
    Guid? InvoiceId,
    DateTime? FromUtc,
    DateTime? ToUtc,
    int Page,
    int PageSize);
