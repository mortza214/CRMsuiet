using CrmSuite.Modules.Billing.Domain.Entities;

namespace CrmSuite.Modules.Billing.Application.Abstractions;

public interface IContractRepository
{
    Task<Contract?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    void Add(Contract contract);
}
