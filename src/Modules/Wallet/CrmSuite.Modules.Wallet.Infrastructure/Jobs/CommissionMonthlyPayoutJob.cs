using CrmSuite.BuildingBlocks.Application.Abstractions;
using CrmSuite.Modules.Orders.Application.Abstractions;
using CrmSuite.Modules.Orders.Domain.Entities;
using CrmSuite.Modules.Orders.Domain.Enums;
using CrmSuite.Modules.Wallet.Application.Abstractions;
using CrmSuite.Modules.Wallet.Application.Services;
using CrmSuite.Modules.Wallet.Domain.Enums;

namespace CrmSuite.Modules.Wallet.Infrastructure.Jobs;

/// <summary>
/// Pays the remaining 11 monthly commission installments after finance approval.
/// It stops when the related customer service is collected or suspended.
/// </summary>
public sealed class CommissionMonthlyPayoutJob
{
    private readonly ICommissionRepository _commissions;
    private readonly ICustomerServiceRepository _customerServices;
    private readonly WalletAccountService _walletAccounts;
    private readonly IWalletUnitOfWork _unitOfWork;
    private readonly IDateTimeProvider _clock;

    public CommissionMonthlyPayoutJob(
        ICommissionRepository commissions,
        ICustomerServiceRepository customerServices,
        WalletAccountService walletAccounts,
        IWalletUnitOfWork unitOfWork,
        IDateTimeProvider clock)
    {
        _commissions = commissions;
        _customerServices = customerServices;
        _walletAccounts = walletAccounts;
        _unitOfWork = unitOfWork;
        _clock = clock;
    }

    public async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        var entries = await _commissions.GetApprovedForPayoutAsync(cancellationToken);
        var now = _clock.UtcNow;

        foreach (var entry in entries)
        {
            if (entry.CustomerServiceId.HasValue)
            {
                var service = await _customerServices.GetByIdAsync(
                    entry.CustomerServiceId.Value,
                    cancellationToken);
                if (service is null
                    || service.Status is CustomerServiceStatus.Collected
                        or CustomerServiceStatus.Suspended)
                {
                    continue;
                }
            }

            foreach (var share in entry.Shares.Where(
                         share => !share.IsCompanyShare && share.UserId.HasValue))
            {
                var wallet = await _walletAccounts.GetOrCreateAsync(
                    share.UserId!.Value,
                    WalletOwnerType.Employee,
                    null,
                    cancellationToken);
                wallet.Deposit(
                    share.Amount,
                    WalletTransactionType.CommissionPayout,
                    entry.Id,
                    $"پورسانت ماهانه - {entry.CustomerName ?? entry.OrderId.ToString()}",
                    null,
                    now);
            }

            entry.MarkInstallmentPaid(now);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
    }
}
