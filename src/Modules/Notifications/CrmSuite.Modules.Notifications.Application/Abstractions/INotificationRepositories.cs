using CrmSuite.Modules.Notifications.Domain.Entities;

namespace CrmSuite.Modules.Notifications.Application.Abstractions;

public interface INotificationTemplateRepository
{
    Task<NotificationTemplate?> GetByCodeAsync(string code, CancellationToken cancellationToken);
}

public interface INotificationLogRepository
{
    Task<NotificationLog?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<IReadOnlyList<NotificationLog>> GetInAppAsync(
        string recipient,
        CancellationToken cancellationToken);

    void Add(NotificationLog log);
}

public interface INotificationUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
