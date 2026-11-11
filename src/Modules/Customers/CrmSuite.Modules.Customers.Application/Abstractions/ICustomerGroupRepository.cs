using CrmSuite.Modules.Customers.Domain.Entities;

namespace CrmSuite.Modules.Customers.Application.Abstractions;

public interface ICustomerGroupRepository
{
    Task<IReadOnlyList<CustomerGroup>> GetAllAsync(CancellationToken cancellationToken);

    Task<CustomerGroup?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<bool> NameExistsAsync(string name, Guid? excludeId, CancellationToken cancellationToken);

    Task<bool> IsInUseAsync(Guid id, CancellationToken cancellationToken);

    void Add(CustomerGroup group);

    void Remove(CustomerGroup group);
}
