using CrmSuite.Modules.Wallet.Domain.Enums;
using WalletEntity = CrmSuite.Modules.Wallet.Domain.Entities.Wallet;

namespace CrmSuite.Modules.Wallet.Application.Abstractions;

public interface IWalletRepository
{
    Task<WalletEntity?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<WalletEntity?> GetByOwnerAsync(
        Guid ownerId,
        WalletOwnerType ownerType,
        CancellationToken cancellationToken);

    void Add(WalletEntity wallet);
}
