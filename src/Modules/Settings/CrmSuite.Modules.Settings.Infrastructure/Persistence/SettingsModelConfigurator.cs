using CrmSuite.BuildingBlocks.Infrastructure.Persistence;
using CrmSuite.Modules.Settings.Infrastructure.Persistence.Configurations;
using Microsoft.EntityFrameworkCore;

namespace CrmSuite.Modules.Settings.Infrastructure.Persistence;

public sealed class SettingsModelConfigurator : IModuleModelConfigurator
{
    public void Configure(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new TaxRuleConfiguration());
        modelBuilder.ApplyConfiguration(new GatewayConfigConfiguration());
    }
}
