using CrmSuite.BuildingBlocks.Infrastructure.Persistence;
using CrmSuite.Modules.Tickets.Application.Abstractions;

namespace CrmSuite.Modules.Tickets.Infrastructure.Persistence;

public sealed class EfTicketUnitOfWork : ITicketUnitOfWork
{
    private readonly AppDbContext _dbContext;

    public EfTicketUnitOfWork(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) =>
        _dbContext.SaveChangesAsync(cancellationToken);
}
