using CrmSuite.BuildingBlocks.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CrmSuite.Modules.Reports.Infrastructure.Persistence;

/// <summary>Entity mappings owned by the Reports module. Filled as the module entities arrive.</summary>
public sealed class ReportsModelConfigurator : IModuleModelConfigurator
{
    public void Configure(ModelBuilder modelBuilder)
    {
        // modelBuilder.ApplyConfiguration(new ExampleConfiguration());
    }
}