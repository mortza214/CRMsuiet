using Microsoft.EntityFrameworkCore;

namespace CrmSuite.BuildingBlocks.Infrastructure.Persistence;

/// <summary>
/// Each module contributes its EF Core mappings here. Keeping this contract in the shared
/// infrastructure project avoids a dependency from the DbContext back to module assemblies.
/// </summary>
public interface IModuleModelConfigurator
{
    void Configure(ModelBuilder modelBuilder);
}
