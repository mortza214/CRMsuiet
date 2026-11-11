using CrmSuite.BuildingBlocks.Infrastructure.Persistence;
using CrmSuite.Modules.Notifications.Application.Abstractions;
using CrmSuite.Modules.Notifications.Domain.Entities;
using CrmSuite.Modules.Notifications.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace CrmSuite.Modules.Notifications.Infrastructure.Persistence.Repositories;

public sealed class NotificationLogRepository : INotificationLogRepository
{
    private readonly AppDbContext _dbContext;

    public NotificationLogRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<NotificationLog?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        _dbContext.Set<NotificationLog>()
            .SingleOrDefaultAsync(log => log.Id == id, cancellationToken);

    public async Task<IReadOnlyList<NotificationLog>> GetInAppAsync(
        string recipient,
        CancellationToken cancellationToken) =>
        await _dbContext.Set<NotificationLog>()
            .Where(log => log.Recipient == recipient
                          && log.Channel == NotificationChannel.InApp
                          && log.Status != NotificationStatus.Failed)
            .OrderByDescending(log => log.CreatedAt)
            .ToListAsync(cancellationToken);

    public void Add(NotificationLog log) => _dbContext.Set<NotificationLog>().Add(log);
}
