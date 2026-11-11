using CrmSuite.Modules.Foundation.Application.Abstractions;
using CrmSuite.Modules.Foundation.Application.Auditing;
using CrmSuite.Modules.Foundation.Infrastructure.Auditing;
using CrmSuite.Modules.Foundation.Infrastructure.Dates;
using CrmSuite.Modules.Foundation.Infrastructure.Sequences;
using CrmSuite.Modules.Foundation.Infrastructure.Settings;
using Microsoft.Extensions.DependencyInjection;

namespace CrmSuite.Modules.Foundation.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddFoundationInfrastructure(this IServiceCollection services)
    {
        services.AddScoped<IAuditScope, AuditScope>();
        services.AddScoped<IJalaliDateService, JalaliDateService>();
        services.AddScoped<IAuditService, AuditService>();
        services.AddScoped<IAuditLogReader, AuditLogReader>();
        services.AddScoped<ISettingService, SettingService>();
        services.AddScoped<ISequenceStore, MySqlSequenceStore>();
        services.AddScoped<ISequenceService, SequenceService>();

        return services;
    }
}
