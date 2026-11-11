using CrmSuite.BuildingBlocks.Infrastructure.Persistence;
using CrmSuite.Modules.Tickets.Application.Abstractions;
using CrmSuite.Modules.Tickets.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace CrmSuite.Modules.Tickets.Infrastructure.Persistence.Repositories;

public sealed class TicketRepository : ITicketRepository
{
    private readonly AppDbContext _dbContext;

    public TicketRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<Ticket?> GetByIdWithDetailsAsync(Guid id, CancellationToken cancellationToken) =>
        _dbContext.Set<Ticket>()
            .Include(ticket => ticket.Messages)
            .Include(ticket => ticket.StatusHistory)
            .SingleOrDefaultAsync(ticket => ticket.Id == id, cancellationToken);

    public void Add(Ticket ticket) => _dbContext.Set<Ticket>().Add(ticket);
}
