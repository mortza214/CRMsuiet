using CrmSuite.BuildingBlocks.Infrastructure.Persistence;
using CrmSuite.Modules.Catalog.Infrastructure.Persistence.Configurations;
using Microsoft.EntityFrameworkCore;

namespace CrmSuite.Modules.Catalog.Infrastructure.Persistence;

/// <summary>Entity mappings owned by the Catalog module.</summary>
public sealed class CatalogModelConfigurator : IModuleModelConfigurator
{
    public void Configure(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new ServiceCategoryConfiguration());
        modelBuilder.ApplyConfiguration(new ServiceConfiguration());
        modelBuilder.ApplyConfiguration(new ServicePriceConfiguration());
        modelBuilder.ApplyConfiguration(new ServiceAddonConfiguration());
        modelBuilder.ApplyConfiguration(new ServiceFormLinkConfiguration());
        modelBuilder.ApplyConfiguration(new DataCenterItemConfiguration());
    }
}
