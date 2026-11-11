using CrmSuite.BuildingBlocks.Infrastructure.Persistence;
using CrmSuite.Modules.Wallet.Application.Abstractions;
using CrmSuite.Modules.Wallet.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace CrmSuite.Modules.Wallet.Infrastructure.Persistence.Repositories;

public sealed class WithdrawalRepository : IWithdrawalRepository
{
    private readonly AppDbContext _dbContext;

    public WithdrawalRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<WithdrawalRequest?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        _dbContext.Set<WithdrawalRequest>()
            .SingleOrDefaultAsync(request => request.Id == id, cancellationToken);

    public void Add(WithdrawalRequest request) => _dbContext.Set<WithdrawalRequest>().Add(request);
}
