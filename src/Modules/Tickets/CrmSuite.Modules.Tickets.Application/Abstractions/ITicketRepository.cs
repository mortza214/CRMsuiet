using CrmSuite.Modules.Tickets.Domain.Entities;

namespace CrmSuite.Modules.Tickets.Application.Abstractions;

public interface ITicketRepository
{
    Task<Ticket?> GetByIdWithDetailsAsync(Guid id, CancellationToken cancellationToken);

    void Add(Ticket ticket);
}
