using CrmSuite.BuildingBlocks.Application.Abstractions;
using CrmSuite.Modules.Foundation.Application.Abstractions;
using CrmSuite.Modules.Wallet.Application.Abstractions;
using CrmSuite.Modules.Wallet.Application.Services;
using CrmSuite.Modules.Wallet.Domain.Enums;

namespace CrmSuite.Modules.Wallet.Infrastructure.Jobs;

public sealed class ProcessMonthlyCommissionsJob
{
    private readonly ICommissionScheduleRepository _schedules;
    private readonly ICommissionRepository _commissions;
    private readonly WalletAccountService _walletAccounts;
    private readonly IWalletUnitOfWork _unitOfWork;
    private readonly IDateTimeProvider _clock;
    private readonly IAuditScope _auditScope;

    public ProcessMonthlyCommissionsJob(
        ICommissionScheduleRepository schedules,
        ICommissionRepository commissions,
        WalletAccountService walletAccounts,
        IWalletUnitOfWork unitOfWork,
        IDateTimeProvider clock,
        IAuditScope auditScope)
    {
        _schedules = schedules;
        _commissions = commissions;
        _walletAccounts = walletAccounts;
        _unitOfWork = unitOfWork;
        _clock = clock;
        _auditScope = auditScope;
    }

    public async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        var now = _clock.UtcNow;
        var dueSchedules = await _schedules.GetDueAsync(now, cancellationToken);

        foreach (var due in dueSchedules)
        {
            var entry = await _commissions.GetByIdWithSchedulesAsync(
                due.CommissionEntryId,
                cancellationToken);

            if (entry is null
                || entry.Status is not (CommissionStatus.Payable or CommissionStatus.Approved))
            {
                continue;
            }

            var schedule = entry.Schedules.SingleOrDefault(item => item.Id == due.Id);
            if (schedule is null || schedule.IsPaid)
            {
                continue;
            }

            var wallet = await _walletAccounts.GetOrCreateAsync(
                entry.EmployeeId,
                WalletOwnerType.Employee,
                null,
                cancellationToken);

            wallet.Deposit(
                schedule.Amount,
                WalletTransactionType.CommissionPayout,
                entry.Id,
                $"پورسانت سفارش {entry.OrderId}",
                null,
                now);

            schedule.MarkPaid(now);

            if (entry.Schedules.All(item => item.IsPaid))
            {
                entry.MarkPaid(null, now);
            }

            await _unitOfWork.SaveChangesAsync(cancellationToken);
            _auditScope.EntityId = entry.Id.ToString();
        }
    }
}
