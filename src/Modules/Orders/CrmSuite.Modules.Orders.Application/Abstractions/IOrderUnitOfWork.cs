namespace CrmSuite.Modules.Orders.Application.Abstractions;

public interface IOrderUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
