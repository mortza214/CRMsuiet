using CrmSuite.BuildingBlocks.Infrastructure.Persistence;
using CrmSuite.Modules.Catalog.Application.Abstractions;
using CrmSuite.Modules.Catalog.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace CrmSuite.Modules.Catalog.Infrastructure.Persistence.Repositories;

public sealed class DataCenterItemRepository : IDataCenterItemRepository
{
    private readonly AppDbContext _dbContext;

    public DataCenterItemRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<DataCenterItem>> GetAllAsync(CancellationToken cancellationToken) =>
        await _dbContext.Set<DataCenterItem>()
            .AsNoTracking()
            .OrderBy(item => item.ItemType)
            .ThenBy(item => item.Name)
            .ToListAsync(cancellationToken);

    public Task<DataCenterItem?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        _dbContext.Set<DataCenterItem>().SingleOrDefaultAsync(item => item.Id == id, cancellationToken);

    public Task<bool> NameExistsAsync(
        string name,
        Guid? excludeId,
        CancellationToken cancellationToken)
    {
        var query = _dbContext.Set<DataCenterItem>().Where(item => item.Name == name.Trim());

        if (excludeId.HasValue)
        {
            query = query.Where(item => item.Id != excludeId.Value);
        }

        return query.AnyAsync(cancellationToken);
    }

    public void Add(DataCenterItem item) => _dbContext.Set<DataCenterItem>().Add(item);

    public void Remove(DataCenterItem item) => _dbContext.Set<DataCenterItem>().Remove(item);
}
