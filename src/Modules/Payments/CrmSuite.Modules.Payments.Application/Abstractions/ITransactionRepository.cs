using CrmSuite.Modules.Payments.Domain.Entities;

namespace CrmSuite.Modules.Payments.Application.Abstractions;

public interface ITransactionRepository
{
    Task<Transaction?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<Transaction?> GetByTrackingCodeAsync(string trackingCode, CancellationToken cancellationToken);

    void Add(Transaction transaction);
}
