using CrmSuite.BuildingBlocks.Infrastructure.Persistence;
using CrmSuite.Modules.Notifications.Application.Abstractions;

namespace CrmSuite.Modules.Notifications.Infrastructure.Persistence;

public sealed class EfNotificationUnitOfWork : INotificationUnitOfWork
{
    private readonly AppDbContext _dbContext;

    public EfNotificationUnitOfWork(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) =>
        _dbContext.SaveChangesAsync(cancellationToken);
}
