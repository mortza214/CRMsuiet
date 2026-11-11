using CrmSuite.Modules.Notifications.Application.Abstractions;
using CrmSuite.Modules.Notifications.Application.Mapping;
using CrmSuite.Modules.Notifications.Infrastructure.Persistence;
using CrmSuite.Modules.Notifications.Infrastructure.Persistence.Repositories;
using CrmSuite.Modules.Notifications.Infrastructure.Senders;
using CrmSuite.Modules.Notifications.Infrastructure.Services;
using CrmSuite.Modules.Notifications.Infrastructure.Sms;
using CrmSuite.Modules.Notifications.Application.Options;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace CrmSuite.Modules.Notifications.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddNotificationsInfrastructure(this IServiceCollection services)
    {
        services.AddScoped<INotificationTemplateRepository, NotificationTemplateRepository>();
        services.AddScoped<INotificationLogRepository, NotificationLogRepository>();
        services.AddScoped<INotificationUnitOfWork, EfNotificationUnitOfWork>();
        services.AddScoped<INotificationSender, InAppNotificationSender>();
        services.AddScoped<INotificationSender, SmsNotificationSender>();
        services.AddScoped<INotificationSender, BaleNotificationSender>();
        services.AddScoped<INotificationService, NotificationService>();
        services.AddScoped<NotificationDtoMapper>();

        services.AddHttpClient<IppanelSmsProvider>((provider, client) =>
        {
            var options = provider.GetRequiredService<IOptions<IppanelOptions>>().Value;
            client.BaseAddress = new Uri(options.BaseUrl);
            client.Timeout = TimeSpan.FromSeconds(15);
        });
        services.AddScoped<ISmsProvider>(provider =>
            provider.GetRequiredService<IppanelSmsProvider>());

        return services;
    }
}
