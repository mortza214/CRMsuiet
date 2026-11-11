using CrmSuite.BuildingBlocks.Infrastructure.Persistence;
using CrmSuite.Modules.Catalog.Domain.Enums;
using CrmSuite.Modules.Wallet.Application.Abstractions;
using CrmSuite.Modules.Wallet.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace CrmSuite.Modules.Wallet.Infrastructure.Persistence.Repositories;

public sealed class CommissionRuleRepository : ICommissionRuleRepository
{
    private readonly AppDbContext _dbContext;

    public CommissionRuleRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<CommissionRule?> GetMatchingAsync(
        Guid? serviceId,
        ServiceType? serviceType,
        CancellationToken cancellationToken)
    {
        var rules = await _dbContext.Set<CommissionRule>()
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        return rules
            .Where(rule => rule.Matches(serviceId, serviceType))
            .OrderBy(rule => rule.ServiceId.HasValue ? 0 : rule.ServiceType.HasValue ? 1 : 2)
            .FirstOrDefault();
    }

    public void Add(CommissionRule rule) => _dbContext.Set<CommissionRule>().Add(rule);
}
