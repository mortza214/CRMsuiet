using CrmSuite.BuildingBlocks.Infrastructure.Persistence;
using CrmSuite.Modules.Settings.Application.Abstractions;
using CrmSuite.Modules.Settings.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace CrmSuite.Modules.Settings.Infrastructure.Persistence.Repositories;

public sealed class TaxRuleRepository : ITaxRuleRepository
{
    private readonly AppDbContext _dbContext;

    public TaxRuleRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<TaxRule>> GetAllAsync(CancellationToken cancellationToken) =>
        await _dbContext.Set<TaxRule>()
            .OrderByDescending(rule => rule.EffectiveFrom)
            .ToListAsync(cancellationToken);

    public Task<TaxRule?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        _dbContext.Set<TaxRule>().SingleOrDefaultAsync(rule => rule.Id == id, cancellationToken);

    public void Add(TaxRule rule) => _dbContext.Set<TaxRule>().Add(rule);
}
