using CrmSuite.BuildingBlocks.Infrastructure.Persistence;
using CrmSuite.Modules.Billing.Application.Abstractions;

namespace CrmSuite.Modules.Billing.Infrastructure.Persistence;

public sealed class EfBillingUnitOfWork : IBillingUnitOfWork
{
    private readonly AppDbContext _dbContext;

    public EfBillingUnitOfWork(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) =>
        _dbContext.SaveChangesAsync(cancellationToken);
}
