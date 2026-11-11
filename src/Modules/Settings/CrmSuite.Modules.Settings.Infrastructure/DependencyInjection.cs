using CrmSuite.Modules.Settings.Application.Abstractions;
using CrmSuite.Modules.Settings.Application.Mapping;
using CrmSuite.Modules.Settings.Infrastructure.Persistence;
using CrmSuite.Modules.Settings.Infrastructure.Persistence.Repositories;
using CrmSuite.Modules.Settings.Infrastructure.Security;
using CrmSuite.Modules.Settings.Infrastructure.Sms;
using CrmSuite.Modules.Notifications.Application.Abstractions;
using Microsoft.Extensions.DependencyInjection;

namespace CrmSuite.Modules.Settings.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddSettingsInfrastructure(this IServiceCollection services)
    {
        services.AddScoped<ITaxRuleRepository, TaxRuleRepository>();
        services.AddScoped<IGatewayConfigRepository, GatewayConfigRepository>();
        services.AddScoped<ISettingsUnitOfWork, EfSettingsUnitOfWork>();
        services.AddScoped<IEncryptionService, AesEncryptionService>();
        services.AddScoped<IIppanelSettingsProvider, IppanelSettingsProvider>();
        services.AddScoped<SettingsDtoMapper>();

        return services;
    }
}
