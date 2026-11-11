using CrmSuite.BuildingBlocks.Infrastructure.Persistence;
using CrmSuite.Modules.Payments.Infrastructure.Persistence.Configurations;
using Microsoft.EntityFrameworkCore;

namespace CrmSuite.Modules.Payments.Infrastructure.Persistence;

/// <summary>Entity mappings owned by the Payments module.</summary>
public sealed class PaymentsModelConfigurator : IModuleModelConfigurator
{
    public void Configure(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new TransactionConfiguration());
        modelBuilder.ApplyConfiguration(new PaymentGatewayTransactionConfiguration());
    }
}
