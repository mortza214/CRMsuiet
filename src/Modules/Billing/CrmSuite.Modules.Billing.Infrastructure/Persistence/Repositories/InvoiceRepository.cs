using CrmSuite.BuildingBlocks.Infrastructure.Persistence;
using CrmSuite.Modules.Billing.Application.Abstractions;
using CrmSuite.Modules.Billing.Domain.Entities;
using CrmSuite.Modules.Billing.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace CrmSuite.Modules.Billing.Infrastructure.Persistence.Repositories;

public sealed class InvoiceRepository : IInvoiceRepository
{
    private readonly AppDbContext _dbContext;

    public InvoiceRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<Invoice?> GetByIdWithItemsAsync(Guid id, CancellationToken cancellationToken) =>
        _dbContext.Set<Invoice>()
            .Include(invoice => invoice.Items)
            .SingleOrDefaultAsync(invoice => invoice.Id == id, cancellationToken);

    public async Task<IReadOnlyList<Invoice>> GetByOrderAsync(
        Guid orderId,
        CancellationToken cancellationToken) =>
        await _dbContext.Set<Invoice>()
            .AsNoTracking()
            .Where(invoice => invoice.OrderId == orderId)
            .OrderByDescending(invoice => invoice.CreatedAt)
            .ToListAsync(cancellationToken);

    public Task<bool> ExistsForOrderAsync(
        Guid orderId,
        BillingInvoiceType type,
        CancellationToken cancellationToken) =>
        _dbContext.Set<Invoice>().AnyAsync(
            invoice => invoice.OrderId == orderId && invoice.Type == type,
            cancellationToken);

    public void Add(Invoice invoice) => _dbContext.Set<Invoice>().Add(invoice);
}
