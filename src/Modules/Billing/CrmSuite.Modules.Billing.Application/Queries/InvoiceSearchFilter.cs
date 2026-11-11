using CrmSuite.Modules.Billing.Domain.Enums;

namespace CrmSuite.Modules.Billing.Application.Queries;

public sealed record InvoiceSearchFilter(
    InvoiceStatus? Status,
    Guid? CustomerId,
    DateTime? FromUtc,
    DateTime? ToUtc,
    string? Search,
    int Page,
    int PageSize);
