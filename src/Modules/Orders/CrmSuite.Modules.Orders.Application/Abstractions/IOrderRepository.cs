using CrmSuite.Modules.Orders.Domain.Entities;

namespace CrmSuite.Modules.Orders.Application.Abstractions;

public interface IOrderRepository
{
    Task<Order?> GetByIdWithDetailsAsync(Guid id, CancellationToken cancellationToken);

    void Add(Order order);
}
