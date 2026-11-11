using CrmSuite.BuildingBlocks.Application.Abstractions;
using CrmSuite.Modules.Foundation.Application.Abstractions;
using CrmSuite.Modules.Wallet.Application.Abstractions;
using CrmSuite.Modules.Wallet.Domain.Enums;
using WalletEntity = CrmSuite.Modules.Wallet.Domain.Entities.Wallet;

namespace CrmSuite.Modules.Wallet.Application.Services;

public sealed class WalletAccountService
{
    private readonly IWalletRepository _wallets;
    private readonly IDateTimeProvider _clock;

    public WalletAccountService(IWalletRepository wallets, IDateTimeProvider clock)
    {
        _wallets = wallets;
        _clock = clock;
    }

    public async Task<WalletEntity> GetOrCreateAsync(
        Guid ownerId,
        WalletOwnerType ownerType,
        Guid? userId,
        CancellationToken cancellationToken)
    {
        var wallet = await _wallets.GetByOwnerAsync(ownerId, ownerType, cancellationToken);
        if (wallet is not null)
        {
            return wallet;
        }

        wallet = new WalletEntity(ownerId, ownerType, userId, _clock.UtcNow);
        _wallets.Add(wallet);
        return wallet;
    }
}
