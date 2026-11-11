using CrmSuite.Modules.Tickets.Application.Abstractions;
using CrmSuite.Modules.Tickets.Application.Mapping;
using CrmSuite.Modules.Tickets.Application.Services;
using CrmSuite.Modules.Tickets.Infrastructure.Notifications;
using CrmSuite.Modules.Tickets.Infrastructure.Persistence;
using CrmSuite.Modules.Tickets.Infrastructure.Persistence.Repositories;
using Microsoft.Extensions.DependencyInjection;

namespace CrmSuite.Modules.Tickets.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddTicketsInfrastructure(this IServiceCollection services)
    {
        services.AddScoped<ITicketRepository, TicketRepository>();
        services.AddScoped<ITicketReadRepository, TicketReadRepository>();
        services.AddScoped<ITicketUnitOfWork, EfTicketUnitOfWork>();
        services.AddScoped<INotificationService, NotificationService>();
        services.AddScoped<StaffAccessService>();
        services.AddScoped<TicketViewService>();
        services.AddScoped<TicketDtoMapper>();

        return services;
    }
}
