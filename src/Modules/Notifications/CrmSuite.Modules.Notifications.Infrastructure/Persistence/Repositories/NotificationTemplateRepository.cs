using CrmSuite.BuildingBlocks.Infrastructure.Persistence;
using CrmSuite.Modules.Notifications.Application.Abstractions;
using CrmSuite.Modules.Notifications.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace CrmSuite.Modules.Notifications.Infrastructure.Persistence.Repositories;

public sealed class NotificationTemplateRepository : INotificationTemplateRepository
{
    private readonly AppDbContext _dbContext;

    public NotificationTemplateRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<NotificationTemplate?> GetByCodeAsync(
        string code,
        CancellationToken cancellationToken) =>
        _dbContext.Set<NotificationTemplate>()
            .SingleOrDefaultAsync(template => template.Code == code.Trim(), cancellationToken);
}
