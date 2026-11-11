namespace CrmSuite.Modules.Tickets.Application.Abstractions;

public interface ITicketUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
