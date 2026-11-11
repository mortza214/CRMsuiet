using CrmSuite.BuildingBlocks.Infrastructure.Persistence;
using CrmSuite.Modules.Notifications.Infrastructure.Persistence.Configurations;
using Microsoft.EntityFrameworkCore;

namespace CrmSuite.Modules.Notifications.Infrastructure.Persistence;

public sealed class NotificationsModelConfigurator : IModuleModelConfigurator
{
    public void Configure(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new NotificationTemplateConfiguration());
        modelBuilder.ApplyConfiguration(new NotificationLogConfiguration());
    }
}
