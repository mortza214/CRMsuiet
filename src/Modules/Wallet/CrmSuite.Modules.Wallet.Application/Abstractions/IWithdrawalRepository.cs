using CrmSuite.Modules.Wallet.Domain.Entities;

namespace CrmSuite.Modules.Wallet.Application.Abstractions;

public interface IWithdrawalRepository
{
    Task<WithdrawalRequest?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    void Add(WithdrawalRequest request);
}
