namespace CrmSuite.Modules.Billing.Application.Abstractions;

public interface IBillingUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
