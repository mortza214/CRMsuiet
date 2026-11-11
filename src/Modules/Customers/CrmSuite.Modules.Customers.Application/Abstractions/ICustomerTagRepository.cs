using CrmSuite.Modules.Customers.Domain.Entities;

namespace CrmSuite.Modules.Customers.Application.Abstractions;

public interface ICustomerTagRepository
{
    Task<IReadOnlyList<CustomerTag>> GetAllAsync(CancellationToken cancellationToken);

    Task<CustomerTag?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<IReadOnlyList<CustomerTag>> GetByIdsAsync(
        IReadOnlyCollection<Guid> ids,
        CancellationToken cancellationToken);

    Task<bool> NameExistsAsync(string name, Guid? excludeId, CancellationToken cancellationToken);

    Task<bool> IsInUseAsync(Guid id, CancellationToken cancellationToken);

    void Add(CustomerTag tag);

    void Remove(CustomerTag tag);
}
