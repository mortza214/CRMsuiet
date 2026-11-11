using CrmSuite.BuildingBlocks.Infrastructure.Persistence;
using CrmSuite.Modules.Workflow.Infrastructure.Persistence.Configurations;
using Microsoft.EntityFrameworkCore;

namespace CrmSuite.Modules.Workflow.Infrastructure.Persistence;

public sealed class WorkflowModelConfigurator : IModuleModelConfigurator
{
    public void Configure(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new WorkflowDefinitionConfiguration());
        modelBuilder.ApplyConfiguration(new WorkflowStateConfiguration());
        modelBuilder.ApplyConfiguration(new WorkflowTransitionConfiguration());
        modelBuilder.ApplyConfiguration(new WorkflowInstanceConfiguration());
        modelBuilder.ApplyConfiguration(new WorkflowTaskConfiguration());
        modelBuilder.ApplyConfiguration(new WorkflowHistoryConfiguration());
    }
}
