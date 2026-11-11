using CrmSuite.BuildingBlocks.Infrastructure.Persistence;
using CrmSuite.Modules.Wallet.Application.Abstractions;
using CrmSuite.Modules.Wallet.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using WalletEntity = CrmSuite.Modules.Wallet.Domain.Entities.Wallet;

namespace CrmSuite.Modules.Wallet.Infrastructure.Persistence.Repositories;

public sealed class WalletRepository : IWalletRepository
{
    private readonly AppDbContext _dbContext;

    public WalletRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<WalletEntity?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        _dbContext.Set<WalletEntity>().SingleOrDefaultAsync(wallet => wallet.Id == id, cancellationToken);

    public Task<WalletEntity?> GetByOwnerAsync(
        Guid ownerId,
        WalletOwnerType ownerType,
        CancellationToken cancellationToken) =>
        _dbContext.Set<WalletEntity>().SingleOrDefaultAsync(
            wallet => wallet.OwnerId == ownerId && wallet.OwnerType == ownerType,
            cancellationToken);

    public void Add(WalletEntity wallet) => _dbContext.Set<WalletEntity>().Add(wallet);
}
