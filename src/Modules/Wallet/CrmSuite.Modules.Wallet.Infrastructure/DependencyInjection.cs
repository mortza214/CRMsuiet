using CrmSuite.Modules.Wallet.Application.Abstractions;
using CrmSuite.Modules.Wallet.Application.Mapping;
using CrmSuite.Modules.Wallet.Application.Services;
using CrmSuite.Modules.Wallet.Infrastructure.Jobs;
using CrmSuite.Modules.Wallet.Infrastructure.Persistence;
using CrmSuite.Modules.Wallet.Infrastructure.Persistence.Repositories;
using CrmSuite.Modules.Wallet.Infrastructure.Services;
using Microsoft.Extensions.DependencyInjection;

namespace CrmSuite.Modules.Wallet.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddWalletInfrastructure(this IServiceCollection services)
    {
        services.AddScoped<IWalletRepository, WalletRepository>();
        services.AddScoped<IWalletReadRepository, WalletReadRepository>();
        services.AddScoped<ICommissionRepository, CommissionRepository>();
        services.AddScoped<ICommissionRuleRepository, CommissionRuleRepository>();
        services.AddScoped<ICommissionScheduleRepository, CommissionScheduleRepository>();
        services.AddScoped<IWithdrawalRepository, WithdrawalRepository>();
        services.AddScoped<IWalletUnitOfWork, EfWalletUnitOfWork>();
        services.AddScoped<ICommissionCalculator, CommissionCalculator>();
        services.AddScoped<WalletAccountService>();
        services.AddScoped<CommissionAllocationService>();
        services.AddScoped<WalletOwnerResolver>();
        services.AddScoped<WalletDtoMapper>();
        services.AddScoped<ProcessMonthlyCommissionsJob>();
        services.AddScoped<CommissionMonthlyPayoutJob>();

        return services;
    }
}
