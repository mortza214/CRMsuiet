using CrmSuite.BuildingBlocks.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CrmSuite.Modules.Commission.Infrastructure.Persistence;

/// <summary>Entity mappings owned by the Commission module. Filled as the module entities arrive.</summary>
public sealed class CommissionModelConfigurator : IModuleModelConfigurator
{
    public void Configure(ModelBuilder modelBuilder)
    {
        // modelBuilder.ApplyConfiguration(new ExampleConfiguration());
    }
}