using CrmSuite.BuildingBlocks.Infrastructure.Persistence;
using CrmSuite.Modules.Orders.Application.Abstractions;
using CrmSuite.Modules.Orders.Domain.Entities;
using CrmSuite.Modules.Orders.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace CrmSuite.Modules.Orders.Infrastructure.Persistence.Repositories;

public sealed class CustomerServiceRepository : ICustomerServiceRepository
{
    private readonly AppDbContext _dbContext;

    public CustomerServiceRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<CustomerService?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken) =>
        _dbContext.Set<CustomerService>()
            .SingleOrDefaultAsync(service => service.Id == id, cancellationToken);

    public Task<CustomerService?> GetByOrderIdAsync(
        Guid orderId,
        CancellationToken cancellationToken) =>
        _dbContext.Set<CustomerService>()
            .SingleOrDefaultAsync(service => service.OrderId == orderId, cancellationToken);

    public async Task<IReadOnlyList<CustomerService>> GetByCustomerAsync(
        Guid customerId,
        CancellationToken cancellationToken) =>
        await _dbContext.Set<CustomerService>()
            .AsNoTracking()
            .Where(service => service.CustomerId == customerId)
            .OrderByDescending(service => service.ActivatedAt)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<CustomerService>> GetDueForInvoicingAsync(
        DateTime utcNow,
        CancellationToken cancellationToken) =>
        await _dbContext.Set<CustomerService>()
            .Where(service =>
                service.Status == CustomerServiceStatus.Active
                && service.NextInvoiceAt.HasValue
                && service.NextInvoiceAt <= utcNow)
            .OrderBy(service => service.NextInvoiceAt)
            .ToListAsync(cancellationToken);

    public void Add(CustomerService service) =>
        _dbContext.Set<CustomerService>().Add(service);
}
