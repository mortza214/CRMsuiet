using CrmSuite.Modules.Billing.Domain.Entities;
using CrmSuite.Modules.Billing.Domain.Enums;

namespace CrmSuite.Modules.Billing.Application.Abstractions;

public interface IInvoiceRepository
{
    Task<Invoice?> GetByIdWithItemsAsync(Guid id, CancellationToken cancellationToken);

    Task<IReadOnlyList<Invoice>> GetByOrderAsync(
        Guid orderId,
        CancellationToken cancellationToken);

    Task<bool> ExistsForOrderAsync(
        Guid orderId,
        BillingInvoiceType type,
        CancellationToken cancellationToken);

    void Add(Invoice invoice);
}
