using CrmSuite.BuildingBlocks.Domain.Results;
using CrmSuite.Modules.Tickets.Application.Dtos;
using CrmSuite.Modules.Tickets.Domain.Enums;
using MediatR;

namespace CrmSuite.Modules.Tickets.Application.Queries;

public sealed record GetTicketsQuery(
    int Page = 1,
    int PageSize = 20,
    TicketStatus? Status = null,
    TicketPriority? Priority = null,
    Guid? DepartmentId = null,
    Guid? CustomerId = null,
    string? Search = null) : IRequest<Result<PagedResult<TicketListItemDto>>>;

public sealed record GetTicketByIdQuery(
    Guid Id,
    bool Portal = false) : IRequest<Result<TicketDto>>;

public sealed record GetMyTicketsQuery : IRequest<Result<IReadOnlyList<TicketListItemDto>>>;
