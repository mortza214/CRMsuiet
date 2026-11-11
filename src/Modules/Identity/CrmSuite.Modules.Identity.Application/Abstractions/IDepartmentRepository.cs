using CrmSuite.Modules.Identity.Domain.Entities;

namespace CrmSuite.Modules.Identity.Application.Abstractions;

public interface IDepartmentRepository
{
    Task<Department?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<IReadOnlyList<Department>> GetAllAsync(CancellationToken cancellationToken);

    Task<IReadOnlyList<Department>> GetByIdsAsync(
        IReadOnlyCollection<Guid> ids,
        CancellationToken cancellationToken);

    Task<bool> NameExistsAsync(
        string name,
        Guid? parentId,
        Guid? excludeId,
        CancellationToken cancellationToken);

    Task<bool> HasChildrenAsync(Guid id, CancellationToken cancellationToken);

    Task<bool> IsDescendantAsync(Guid id, Guid potentialParentId, CancellationToken cancellationToken);

    Task<int> CountUsersAsync(Guid id, CancellationToken cancellationToken);

    void Add(Department department);

    void Remove(Department department);
}
