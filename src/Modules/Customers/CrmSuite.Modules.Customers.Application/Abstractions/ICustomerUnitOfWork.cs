namespace CrmSuite.Modules.Customers.Application.Abstractions;

public interface ICustomerUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
