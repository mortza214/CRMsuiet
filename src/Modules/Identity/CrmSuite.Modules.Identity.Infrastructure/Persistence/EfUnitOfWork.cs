using CrmSuite.BuildingBlocks.Infrastructure.Persistence;
using CrmSuite.Modules.Identity.Application.Abstractions;

namespace CrmSuite.Modules.Identity.Infrastructure.Persistence;

public sealed class EfUnitOfWork : IUnitOfWork
{
    private readonly AppDbContext _dbContext;

    public EfUnitOfWork(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) =>
        _dbContext.SaveChangesAsync(cancellationToken);
}
