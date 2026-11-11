using CrmSuite.Modules.Tickets.Domain.Enums;

namespace CrmSuite.Modules.Tickets.Application.Queries;

public sealed record TicketSearchFilter(
    TicketStatus? Status,
    TicketPriority? Priority,
    Guid? DepartmentId,
    Guid? CustomerId,
    string? Search,
    IReadOnlyCollection<Guid>? AllowedDepartmentIds,
    int Page,
    int PageSize);
