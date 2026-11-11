using CrmSuite.BuildingBlocks.Infrastructure.Persistence;
using CrmSuite.Modules.Customers.Application.Abstractions;
using CrmSuite.Modules.Customers.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace CrmSuite.Modules.Customers.Infrastructure.Persistence.Repositories;

public sealed class CustomerTagRepository : ICustomerTagRepository
{
    private readonly AppDbContext _dbContext;

    public CustomerTagRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<CustomerTag>> GetAllAsync(CancellationToken cancellationToken) =>
        await _dbContext.Set<CustomerTag>()
            .AsNoTracking()
            .OrderBy(tag => tag.Name)
            .ToListAsync(cancellationToken);

    public Task<CustomerTag?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        _dbContext.Set<CustomerTag>().SingleOrDefaultAsync(tag => tag.Id == id, cancellationToken);

    public async Task<IReadOnlyList<CustomerTag>> GetByIdsAsync(
        IReadOnlyCollection<Guid> ids,
        CancellationToken cancellationToken)
    {
        if (ids.Count == 0)
        {
            return [];
        }

        return await _dbContext.Set<CustomerTag>()
            .Where(tag => ids.Contains(tag.Id))
            .ToListAsync(cancellationToken);
    }

    public Task<bool> NameExistsAsync(
        string name,
        Guid? excludeId,
        CancellationToken cancellationToken)
    {
        var query = _dbContext.Set<CustomerTag>().Where(tag => tag.Name == name.Trim());

        if (excludeId.HasValue)
        {
            query = query.Where(tag => tag.Id != excludeId.Value);
        }

        return query.AnyAsync(cancellationToken);
    }

    public Task<bool> IsInUseAsync(Guid id, CancellationToken cancellationToken) =>
        _dbContext.Set<CustomerTagMap>().AnyAsync(map => map.TagId == id, cancellationToken);

    public void Add(CustomerTag tag) => _dbContext.Set<CustomerTag>().Add(tag);

    public void Remove(CustomerTag tag) => _dbContext.Set<CustomerTag>().Remove(tag);
}
