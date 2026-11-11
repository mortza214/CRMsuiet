using CrmSuite.BuildingBlocks.Infrastructure.Persistence;
using CrmSuite.Modules.Payments.Application.Abstractions;

namespace CrmSuite.Modules.Payments.Infrastructure.Persistence;

public sealed class EfPaymentUnitOfWork : IPaymentUnitOfWork
{
    private readonly AppDbContext _dbContext;

    public EfPaymentUnitOfWork(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) =>
        _dbContext.SaveChangesAsync(cancellationToken);
}
