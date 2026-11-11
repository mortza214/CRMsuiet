using CrmSuite.Modules.Customers.Domain.Entities;

namespace CrmSuite.Modules.Customers.Application.Abstractions;

public interface ICustomerRepository
{
    Task<Customer?> GetByIdAsync(Guid id, bool includeDetails, CancellationToken cancellationToken);

    Task<Customer?> GetByMobileAsync(string mobile, bool includeDetails, CancellationToken cancellationToken);

    Task<Customer?> GetByUserIdAsync(Guid userId, bool includeDetails, CancellationToken cancellationToken);

    Task<bool> MobileExistsAsync(string mobile, Guid? excludeId, CancellationToken cancellationToken);

    Task<bool> NationalIdExistsAsync(string nationalId, Guid? excludeId, CancellationToken cancellationToken);

    Task<IReadOnlyList<Customer>> GetSubCustomersAsync(Guid parentId, CancellationToken cancellationToken);

    Task<bool> IsDescendantAsync(Guid id, Guid potentialParentId, CancellationToken cancellationToken);

    void Add(Customer customer);

    void Remove(Customer customer);
}
