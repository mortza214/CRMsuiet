namespace CrmSuite.Modules.Payments.Application.Abstractions;

public interface IPaymentUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
