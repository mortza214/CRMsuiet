using CrmSuite.BuildingBlocks.Infrastructure.Persistence;
using CrmSuite.Modules.Customers.Application.Abstractions;
using CrmSuite.Modules.Customers.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace CrmSuite.Modules.Customers.Infrastructure.Persistence.Repositories;

public sealed class CustomerGroupRepository : ICustomerGroupRepository
{
    private readonly AppDbContext _dbContext;

    public CustomerGroupRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<CustomerGroup>> GetAllAsync(CancellationToken cancellationToken) =>
        await _dbContext.Set<CustomerGroup>()
            .AsNoTracking()
            .OrderBy(group => group.Name)
            .ToListAsync(cancellationToken);

    public Task<CustomerGroup?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        _dbContext.Set<CustomerGroup>().SingleOrDefaultAsync(group => group.Id == id, cancellationToken);

    public Task<bool> NameExistsAsync(
        string name,
        Guid? excludeId,
        CancellationToken cancellationToken)
    {
        var query = _dbContext.Set<CustomerGroup>().Where(group => group.Name == name.Trim());

        if (excludeId.HasValue)
        {
            query = query.Where(group => group.Id != excludeId.Value);
        }

        return query.AnyAsync(cancellationToken);
    }

    public Task<bool> IsInUseAsync(Guid id, CancellationToken cancellationToken) =>
        _dbContext.Set<Customer>().AnyAsync(customer => customer.GroupId == id, cancellationToken);

    public void Add(CustomerGroup group) => _dbContext.Set<CustomerGroup>().Add(group);

    public void Remove(CustomerGroup group) => _dbContext.Set<CustomerGroup>().Remove(group);
}
