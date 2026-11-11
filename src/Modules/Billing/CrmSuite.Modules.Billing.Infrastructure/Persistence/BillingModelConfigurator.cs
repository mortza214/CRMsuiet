using CrmSuite.BuildingBlocks.Infrastructure.Persistence;
using CrmSuite.Modules.Billing.Infrastructure.Persistence.Configurations;
using Microsoft.EntityFrameworkCore;

namespace CrmSuite.Modules.Billing.Infrastructure.Persistence;

/// <summary>Entity mappings owned by the Billing module.</summary>
public sealed class BillingModelConfigurator : IModuleModelConfigurator
{
    public void Configure(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new InvoiceConfiguration());
        modelBuilder.ApplyConfiguration(new InvoiceItemConfiguration());
        modelBuilder.ApplyConfiguration(new ContractConfiguration());
    }
}
