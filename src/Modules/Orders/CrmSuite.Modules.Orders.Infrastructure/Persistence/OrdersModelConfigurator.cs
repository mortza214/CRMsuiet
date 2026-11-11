using CrmSuite.BuildingBlocks.Infrastructure.Persistence;
using CrmSuite.Modules.Orders.Infrastructure.Persistence.Configurations;
using Microsoft.EntityFrameworkCore;

namespace CrmSuite.Modules.Orders.Infrastructure.Persistence;

/// <summary>Entity mappings owned by the Orders module.</summary>
public sealed class OrdersModelConfigurator : IModuleModelConfigurator
{
    public void Configure(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new OrderConfiguration());
        modelBuilder.ApplyConfiguration(new OrderItemConfiguration());
        modelBuilder.ApplyConfiguration(new OrderStatusHistoryConfiguration());
        modelBuilder.ApplyConfiguration(new CustomerServiceConfiguration());
    }
}
