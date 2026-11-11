using CrmSuite.BuildingBlocks.Infrastructure.Persistence;
using CrmSuite.Modules.Wallet.Application.Abstractions;
using CrmSuite.Modules.Wallet.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace CrmSuite.Modules.Wallet.Infrastructure.Persistence.Repositories;

public sealed class CommissionRepository : ICommissionRepository
{
    private readonly AppDbContext _dbContext;

    public CommissionRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<CommissionEntry?> GetByIdWithSchedulesAsync(
        Guid id,
        CancellationToken cancellationToken) =>
        _dbContext.Set<CommissionEntry>()
            .Include(entry => entry.Schedules)
            .Include(entry => entry.Shares)
            .SingleOrDefaultAsync(entry => entry.Id == id, cancellationToken);

    public Task<bool> ExistsForOrderAsync(Guid orderId, CancellationToken cancellationToken) =>
        _dbContext.Set<CommissionEntry>().AnyAsync(entry => entry.OrderId == orderId, cancellationToken);

    public async Task<IReadOnlyList<CommissionEntry>> GetApprovedForPayoutAsync(
        CancellationToken cancellationToken) =>
        await _dbContext.Set<CommissionEntry>()
            .Include(entry => entry.Shares)
            .Where(entry => entry.Status == Domain.Enums.CommissionStatus.Approved
                            && entry.PaidInstallments < 12)
            .ToListAsync(cancellationToken);

    public void Add(CommissionEntry entry) => _dbContext.Set<CommissionEntry>().Add(entry);
}
