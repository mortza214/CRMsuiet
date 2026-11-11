using CrmSuite.BuildingBlocks.Infrastructure.Persistence;
using CrmSuite.Modules.Catalog.Application.Abstractions;
using CrmSuite.Modules.Catalog.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace CrmSuite.Modules.Catalog.Infrastructure.Persistence.Repositories;

public sealed class ServiceRepository : IServiceRepository
{
    private readonly AppDbContext _dbContext;

    public ServiceRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<Service?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        _dbContext.Set<Service>()
            .Include(service => service.Category)
            .SingleOrDefaultAsync(service => service.Id == id, cancellationToken);

    public Task<Service?> GetByIdWithPricesAsync(Guid id, CancellationToken cancellationToken) =>
        _dbContext.Set<Service>()
            .Include(service => service.Category)
            .Include(service => service.Prices)
            .SingleOrDefaultAsync(service => service.Id == id, cancellationToken);

    public Task<Service?> GetByIdWithDetailsAsync(
        Guid id,
        CancellationToken cancellationToken) =>
        _dbContext.Set<Service>()
            .Include(service => service.Category)
            .Include(service => service.Prices)
            .SingleOrDefaultAsync(service => service.Id == id, cancellationToken);

    public Task<bool> CodeExistsAsync(
        string code,
        Guid? excludeId,
        CancellationToken cancellationToken)
    {
        var normalized = code.Trim().ToUpperInvariant();
        var query = _dbContext.Set<Service>().Where(service => service.Code == normalized);

        if (excludeId.HasValue)
        {
            query = query.Where(service => service.Id != excludeId.Value);
        }

        return query.AnyAsync(cancellationToken);
    }

    public Task<bool> CategoryHasServicesAsync(Guid categoryId, CancellationToken cancellationToken) =>
        _dbContext.Set<Service>().AnyAsync(service => service.CategoryId == categoryId, cancellationToken);

    public Task<ServicePrice?> GetEffectivePriceAsync(
        Guid serviceId,
        DateTime utcDate,
        CancellationToken cancellationToken)
    {
        var date = DateTime.SpecifyKind(utcDate, DateTimeKind.Utc);

        return _dbContext.Set<ServicePrice>()
            .Where(price => price.ServiceId == serviceId
                            && price.EffectiveFrom <= date
                            && (price.EffectiveTo == null || price.EffectiveTo > date))
            .OrderByDescending(price => price.EffectiveFrom)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public void Add(Service service) => _dbContext.Set<Service>().Add(service);
}
