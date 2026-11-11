using CrmSuite.BuildingBlocks.Infrastructure.Persistence;
using CrmSuite.Modules.Catalog.Application.Abstractions;
using CrmSuite.Modules.Catalog.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace CrmSuite.Modules.Catalog.Infrastructure.Persistence.Repositories;

public sealed class ServiceFormLinkRepository : IServiceFormLinkRepository
{
    private readonly AppDbContext _dbContext;

    public ServiceFormLinkRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<ServiceFormLink>> GetByServiceAsync(
        Guid serviceId,
        CancellationToken cancellationToken) =>
        await _dbContext.Set<ServiceFormLink>()
            .Where(link => link.ServiceId == serviceId)
            .OrderBy(link => link.SortOrder)
            .ToListAsync(cancellationToken);

    public Task<ServiceFormLink?> GetAsync(
        Guid serviceId,
        Guid formDefinitionId,
        CancellationToken cancellationToken) =>
        _dbContext.Set<ServiceFormLink>()
            .SingleOrDefaultAsync(
                link => link.ServiceId == serviceId
                        && link.FormDefinitionId == formDefinitionId,
                cancellationToken);

    public void Add(ServiceFormLink link) =>
        _dbContext.Set<ServiceFormLink>().Add(link);

    public void Remove(ServiceFormLink link) =>
        _dbContext.Set<ServiceFormLink>().Remove(link);
}
