using CrmSuite.BuildingBlocks.Infrastructure.Persistence;
using CrmSuite.Modules.Wallet.Infrastructure.Persistence.Configurations;
using Microsoft.EntityFrameworkCore;

namespace CrmSuite.Modules.Wallet.Infrastructure.Persistence;

/// <summary>Entity mappings owned by the Wallet and Commission modules.</summary>
public sealed class WalletModelConfigurator : IModuleModelConfigurator
{
    public void Configure(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new WalletConfiguration());
        modelBuilder.ApplyConfiguration(new WalletTransactionConfiguration());
        modelBuilder.ApplyConfiguration(new WithdrawalRequestConfiguration());
        modelBuilder.ApplyConfiguration(new CommissionRuleConfiguration());
        modelBuilder.ApplyConfiguration(new CommissionEntryConfiguration());
        modelBuilder.ApplyConfiguration(new CommissionScheduleConfiguration());
        modelBuilder.ApplyConfiguration(new CommissionShareConfiguration());
    }
}
