using CrmSuite.BuildingBlocks.Infrastructure.Persistence;
using CrmSuite.Modules.Tickets.Application.Abstractions;
using CrmSuite.Modules.Tickets.Domain.Entities;

namespace CrmSuite.Modules.Tickets.Infrastructure.Notifications;

public sealed class NotificationService : INotificationService
{
    private readonly AppDbContext _dbContext;

    public NotificationService(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task NotifyDepartmentAsync(
        Guid departmentId,
        string title,
        string body,
        CancellationToken cancellationToken)
    {
        _dbContext.Set<Notification>().Add(new Notification(
            departmentId,
            null,
            title,
            body,
            DateTime.UtcNow));

        return Task.CompletedTask;
    }
}
