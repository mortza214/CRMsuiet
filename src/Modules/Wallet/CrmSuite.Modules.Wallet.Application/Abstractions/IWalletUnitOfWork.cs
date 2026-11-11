namespace CrmSuite.Modules.Wallet.Application.Abstractions;

public interface IWalletUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
