using CrmSuite.Modules.Orders.Domain.Entities;

namespace CrmSuite.Modules.Orders.Application.Abstractions;

public interface ICustomerServiceRepository
{
    Task<CustomerService?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<CustomerService?> GetByOrderIdAsync(Guid orderId, CancellationToken cancellationToken);

    Task<IReadOnlyList<CustomerService>> GetByCustomerAsync(
        Guid customerId,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<CustomerService>> GetDueForInvoicingAsync(
        DateTime utcNow,
        CancellationToken cancellationToken);

    void Add(CustomerService service);
}
