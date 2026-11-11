using CrmSuite.BuildingBlocks.Infrastructure.Persistence;
using CrmSuite.Modules.Forms.Application.Abstractions;

namespace CrmSuite.Modules.Forms.Infrastructure.Persistence;

public sealed class EfFormUnitOfWork : IFormUnitOfWork
{
    private readonly AppDbContext _dbContext;

    public EfFormUnitOfWork(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) =>
        _dbContext.SaveChangesAsync(cancellationToken);
}
