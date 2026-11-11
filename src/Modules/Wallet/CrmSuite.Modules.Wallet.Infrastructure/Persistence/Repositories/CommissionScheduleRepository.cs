using CrmSuite.BuildingBlocks.Infrastructure.Persistence;
using CrmSuite.Modules.Wallet.Application.Abstractions;
using CrmSuite.Modules.Wallet.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace CrmSuite.Modules.Wallet.Infrastructure.Persistence.Repositories;

public sealed class CommissionScheduleRepository : ICommissionScheduleRepository
{
    private readonly AppDbContext _dbContext;

    public CommissionScheduleRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<CommissionSchedule>> GetDueAsync(
        DateTime utcNow,
        CancellationToken cancellationToken) =>
        await _dbContext.Set<CommissionSchedule>()
            .Where(schedule => !schedule.IsPaid && schedule.DueDate <= utcNow)
            .OrderBy(schedule => schedule.DueDate)
            .ToListAsync(cancellationToken);

    public Task<int> CountUnpaidAsync(
        Guid commissionEntryId,
        CancellationToken cancellationToken) =>
        _dbContext.Set<CommissionSchedule>()
            .CountAsync(
                schedule => schedule.CommissionEntryId == commissionEntryId && !schedule.IsPaid,
                cancellationToken);
}
