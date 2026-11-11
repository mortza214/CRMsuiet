using CrmSuite.BuildingBlocks.Infrastructure.Persistence;
using CrmSuite.Modules.Settings.Application.Abstractions;

namespace CrmSuite.Modules.Settings.Infrastructure.Persistence;

public sealed class EfSettingsUnitOfWork : ISettingsUnitOfWork
{
    private readonly AppDbContext _dbContext;

    public EfSettingsUnitOfWork(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) =>
        _dbContext.SaveChangesAsync(cancellationToken);
}
