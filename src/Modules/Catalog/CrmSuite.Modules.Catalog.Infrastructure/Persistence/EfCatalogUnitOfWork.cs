using CrmSuite.BuildingBlocks.Infrastructure.Persistence;
using CrmSuite.Modules.Catalog.Application.Abstractions;

namespace CrmSuite.Modules.Catalog.Infrastructure.Persistence;

public sealed class EfCatalogUnitOfWork : ICatalogUnitOfWork
{
    private readonly AppDbContext _dbContext;

    public EfCatalogUnitOfWork(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) =>
        _dbContext.SaveChangesAsync(cancellationToken);
}
