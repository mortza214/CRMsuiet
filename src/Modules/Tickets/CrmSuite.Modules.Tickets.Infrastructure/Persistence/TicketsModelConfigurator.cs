using CrmSuite.BuildingBlocks.Infrastructure.Persistence;
using CrmSuite.Modules.Tickets.Infrastructure.Persistence.Configurations;
using Microsoft.EntityFrameworkCore;

namespace CrmSuite.Modules.Tickets.Infrastructure.Persistence;

/// <summary>Entity mappings owned by the Tickets module.</summary>
public sealed class TicketsModelConfigurator : IModuleModelConfigurator
{
    public void Configure(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new TicketConfiguration());
        modelBuilder.ApplyConfiguration(new TicketMessageConfiguration());
        modelBuilder.ApplyConfiguration(new TicketStatusHistoryConfiguration());
        modelBuilder.ApplyConfiguration(new NotificationConfiguration());
    }
}
