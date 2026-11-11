using CrmSuite.BuildingBlocks.Infrastructure.Persistence;
using CrmSuite.Modules.Wallet.Application.Abstractions;

namespace CrmSuite.Modules.Wallet.Infrastructure.Persistence;

public sealed class EfWalletUnitOfWork : IWalletUnitOfWork
{
    private readonly AppDbContext _dbContext;

    public EfWalletUnitOfWork(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) =>
        _dbContext.SaveChangesAsync(cancellationToken);
}
