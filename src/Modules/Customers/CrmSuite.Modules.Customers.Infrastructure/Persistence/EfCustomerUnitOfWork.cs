using CrmSuite.BuildingBlocks.Infrastructure.Persistence;
using CrmSuite.Modules.Customers.Application.Abstractions;

namespace CrmSuite.Modules.Customers.Infrastructure.Persistence;

public sealed class EfCustomerUnitOfWork : ICustomerUnitOfWork
{
    private readonly AppDbContext _dbContext;

    public EfCustomerUnitOfWork(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) =>
        _dbContext.SaveChangesAsync(cancellationToken);
}
