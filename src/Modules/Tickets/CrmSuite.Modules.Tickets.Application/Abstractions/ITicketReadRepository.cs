using CrmSuite.BuildingBlocks.Domain.Results;
using CrmSuite.Modules.Tickets.Application.Dtos;
using CrmSuite.Modules.Tickets.Application.Queries;

namespace CrmSuite.Modules.Tickets.Application.Abstractions;

public interface ITicketReadRepository
{
    Task<PagedResult<TicketListItemDto>> SearchAsync(
        TicketSearchFilter filter,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<TicketListItemDto>> GetCustomerTicketsAsync(
        Guid customerId,
        CancellationToken cancellationToken);
}
