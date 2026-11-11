using CrmSuite.BuildingBlocks.Infrastructure.Persistence;
using CrmSuite.Modules.Forms.Infrastructure.Persistence.Configurations;
using Microsoft.EntityFrameworkCore;

namespace CrmSuite.Modules.Forms.Infrastructure.Persistence;

public sealed class FormsModelConfigurator : IModuleModelConfigurator
{
    public void Configure(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new FormTemplateConfiguration());
        modelBuilder.ApplyConfiguration(new FormFieldConfiguration());
        modelBuilder.ApplyConfiguration(new FormSubmissionConfiguration());
        modelBuilder.ApplyConfiguration(new FormSubmissionItemConfiguration());
        modelBuilder.ApplyConfiguration(new FormSubmissionHistoryConfiguration());
    }
}
