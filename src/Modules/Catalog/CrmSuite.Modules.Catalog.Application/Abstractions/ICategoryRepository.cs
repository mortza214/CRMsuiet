using CrmSuite.Modules.Catalog.Domain.Entities;

namespace CrmSuite.Modules.Catalog.Application.Abstractions;

public interface ICategoryRepository
{
    Task<IReadOnlyList<ServiceCategory>> GetAllAsync(CancellationToken cancellationToken);

    Task<ServiceCategory?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<bool> NameExistsAsync(string name, Guid? parentId, Guid? excludeId, CancellationToken cancellationToken);

    Task<bool> HasChildrenAsync(Guid id, CancellationToken cancellationToken);

    Task<bool> HasServicesAsync(Guid id, CancellationToken cancellationToken);

    Task<bool> IsDescendantAsync(Guid id, Guid potentialParentId, CancellationToken cancellationToken);

    void Add(ServiceCategory category);

    void Remove(ServiceCategory category);
}
