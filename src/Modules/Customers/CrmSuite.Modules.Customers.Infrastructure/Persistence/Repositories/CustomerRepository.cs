using CrmSuite.BuildingBlocks.Infrastructure.Persistence;
using CrmSuite.Modules.Customers.Application.Abstractions;
using CrmSuite.Modules.Customers.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace CrmSuite.Modules.Customers.Infrastructure.Persistence.Repositories;

public sealed class CustomerRepository : ICustomerRepository
{
    private readonly AppDbContext _dbContext;

    public CustomerRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<Customer?> GetByIdAsync(
        Guid id,
        bool includeDetails,
        CancellationToken cancellationToken)
    {
        var query = includeDetails ? WithDetails() : _dbContext.Set<Customer>();
        return query.SingleOrDefaultAsync(customer => customer.Id == id, cancellationToken);
    }

    public Task<Customer?> GetByMobileAsync(
        string mobile,
        bool includeDetails,
        CancellationToken cancellationToken)
    {
        var normalized = mobile.Trim();
        var query = includeDetails ? WithDetails() : _dbContext.Set<Customer>();

        return query.SingleOrDefaultAsync(
            customer => customer.Mobile == normalized,
            cancellationToken);
    }

    public Task<Customer?> GetByUserIdAsync(
        Guid userId,
        bool includeDetails,
        CancellationToken cancellationToken)
    {
        var query = includeDetails ? WithDetails() : _dbContext.Set<Customer>();

        return query.SingleOrDefaultAsync(
            customer => customer.UserId == userId,
            cancellationToken);
    }

    public Task<bool> MobileExistsAsync(
        string mobile,
        Guid? excludeId,
        CancellationToken cancellationToken)
    {
        var normalized = mobile.Trim();
        var query = _dbContext.Set<Customer>().Where(customer => customer.Mobile == normalized);

        if (excludeId.HasValue)
        {
            query = query.Where(customer => customer.Id != excludeId.Value);
        }

        return query.AnyAsync(cancellationToken);
    }

    public Task<bool> NationalIdExistsAsync(
        string nationalId,
        Guid? excludeId,
        CancellationToken cancellationToken)
    {
        var normalized = nationalId.Trim();
        var query = _dbContext.Set<Customer>().Where(customer => customer.NationalId == normalized);

        if (excludeId.HasValue)
        {
            query = query.Where(customer => customer.Id != excludeId.Value);
        }

        return query.AnyAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Customer>> GetSubCustomersAsync(
        Guid parentId,
        CancellationToken cancellationToken) =>
        await WithDetails()
            .Where(customer => customer.ParentCustomerId == parentId)
            .OrderBy(customer => customer.CreatedAt)
            .ToListAsync(cancellationToken);

    public async Task<bool> IsDescendantAsync(
        Guid id,
        Guid potentialParentId,
        CancellationToken cancellationToken)
    {
        var current = potentialParentId;
        var visited = new HashSet<Guid>();

        while (visited.Add(current))
        {
            if (current == id)
            {
                return true;
            }

            var parentId = await _dbContext.Set<Customer>()
                .Where(customer => customer.Id == current)
                .Select(customer => customer.ParentCustomerId)
                .FirstOrDefaultAsync(cancellationToken);

            if (!parentId.HasValue)
            {
                return false;
            }

            current = parentId.Value;
        }

        return false;
    }

    public void Add(Customer customer) => _dbContext.Set<Customer>().Add(customer);

    public void Remove(Customer customer) => _dbContext.Set<Customer>().Remove(customer);

    private IQueryable<Customer> WithDetails() =>
        _dbContext.Set<Customer>()
            .Include(customer => customer.Group)
            .Include(customer => customer.TagMaps)
            .ThenInclude(map => map.Tag);
}
