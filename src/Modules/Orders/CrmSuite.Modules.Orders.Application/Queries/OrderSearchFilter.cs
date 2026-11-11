using CrmSuite.Modules.Orders.Domain.Enums;

namespace CrmSuite.Modules.Orders.Application.Queries;

public sealed record OrderSearchFilter(
    OrderStatus? Status,
    Guid? CustomerId,
    Guid? SalesExpertId,
    DateTime? FromUtc,
    DateTime? ToUtc,
    string? Search,
    int Page,
    int PageSize);
