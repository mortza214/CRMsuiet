using CrmSuite.BuildingBlocks.Infrastructure.Persistence;
using CrmSuite.Modules.Catalog.Application.Abstractions;
using CrmSuite.Modules.Catalog.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace CrmSuite.Modules.Catalog.Infrastructure.Persistence.Repositories;

public sealed class AddonRepository : IAddonRepository
{
    private readonly AppDbContext _dbContext;

    public AddonRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<ServiceAddon>> GetAllAsync(CancellationToken cancellationToken) =>
        await _dbContext.Set<ServiceAddon>()
            .AsNoTracking()
            .OrderBy(addon => addon.Name)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<ServiceAddon>> GetAvailableForServiceAsync(
        Guid serviceId,
        CancellationToken cancellationToken) =>
        await _dbContext.Set<ServiceAddon>()
            .Where(addon =>
                addon.IsActive
                && (!addon.ServiceId.HasValue || addon.ServiceId == serviceId))
            .OrderBy(addon => addon.SortOrder)
            .ThenBy(addon => addon.Name)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<ServiceAddon>> GetByIdsAsync(
        IReadOnlyCollection<Guid> ids,
        CancellationToken cancellationToken) =>
        await _dbContext.Set<ServiceAddon>()
            .Where(addon => ids.Contains(addon.Id))
            .ToListAsync(cancellationToken);

    public Task<ServiceAddon?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        _dbContext.Set<ServiceAddon>().SingleOrDefaultAsync(addon => addon.Id == id, cancellationToken);

    public Task<bool> NameExistsAsync(
        string name,
        Guid? excludeId,
        CancellationToken cancellationToken)
    {
        var query = _dbContext.Set<ServiceAddon>().Where(addon => addon.Name == name.Trim());

        if (excludeId.HasValue)
        {
            query = query.Where(addon => addon.Id != excludeId.Value);
        }

        return query.AnyAsync(cancellationToken);
    }

    public void Add(ServiceAddon addon) => _dbContext.Set<ServiceAddon>().Add(addon);

    public void Remove(ServiceAddon addon) => _dbContext.Set<ServiceAddon>().Remove(addon);
}
