using CrmSuite.BuildingBlocks.Infrastructure.Persistence;
using CrmSuite.Modules.Orders.Application.Abstractions;

namespace CrmSuite.Modules.Orders.Infrastructure.Persistence;

public sealed class EfOrderUnitOfWork : IOrderUnitOfWork
{
    private readonly AppDbContext _dbContext;

    public EfOrderUnitOfWork(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) =>
        _dbContext.SaveChangesAsync(cancellationToken);
}
