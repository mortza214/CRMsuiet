using CrmSuite.Modules.Wallet.Domain.Entities;

namespace CrmSuite.Modules.Wallet.Application.Abstractions;

public interface ICommissionScheduleRepository
{
    Task<IReadOnlyList<CommissionSchedule>> GetDueAsync(
        DateTime utcNow,
        CancellationToken cancellationToken);

    Task<int> CountUnpaidAsync(Guid commissionEntryId, CancellationToken cancellationToken);
}
