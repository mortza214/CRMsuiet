using CrmSuite.BuildingBlocks.Infrastructure.Persistence;
using CrmSuite.Modules.Workflow.Application.Abstractions;

namespace CrmSuite.Modules.Workflow.Infrastructure.Persistence;

public sealed class EfWorkflowUnitOfWork : IWorkflowUnitOfWork
{
    private readonly AppDbContext _dbContext;

    public EfWorkflowUnitOfWork(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) =>
        _dbContext.SaveChangesAsync(cancellationToken);
}
