using CrmSuite.BuildingBlocks.Infrastructure.Persistence;
using CrmSuite.Modules.Customers.Infrastructure.Persistence.Configurations;
using Microsoft.EntityFrameworkCore;

namespace CrmSuite.Modules.Customers.Infrastructure.Persistence;

/// <summary>Entity mappings owned by the Customers module.</summary>
public sealed class CustomersModelConfigurator : IModuleModelConfigurator
{
    public void Configure(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new CustomerConfiguration());
        modelBuilder.ApplyConfiguration(new CustomerGroupConfiguration());
        modelBuilder.ApplyConfiguration(new CustomerTagConfiguration());
        modelBuilder.ApplyConfiguration(new CustomerTagMapConfiguration());
        modelBuilder.ApplyConfiguration(new CustomerOtpConfiguration());
    }
}
