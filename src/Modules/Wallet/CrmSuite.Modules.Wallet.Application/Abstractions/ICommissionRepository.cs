using CrmSuite.Modules.Wallet.Domain.Entities;

namespace CrmSuite.Modules.Wallet.Application.Abstractions;

public interface ICommissionRepository
{
    Task<CommissionEntry?> GetByIdWithSchedulesAsync(Guid id, CancellationToken cancellationToken);

    Task<bool> ExistsForOrderAsync(Guid orderId, CancellationToken cancellationToken);

    Task<IReadOnlyList<CommissionEntry>> GetApprovedForPayoutAsync(
        CancellationToken cancellationToken);

    void Add(CommissionEntry entry);
}
