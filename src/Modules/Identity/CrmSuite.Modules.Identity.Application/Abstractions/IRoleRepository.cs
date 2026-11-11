using CrmSuite.Modules.Identity.Domain.Entities;

namespace CrmSuite.Modules.Identity.Application.Abstractions;

public interface IRoleRepository
{
    Task<Role?> GetByIdWithPermissionsAsync(Guid id, CancellationToken cancellationToken);

    Task<IReadOnlyList<Role>> GetAllAsync(bool includePermissions, CancellationToken cancellationToken);

    Task<IReadOnlyList<Role>> GetByIdsAsync(
        IReadOnlyCollection<Guid> ids,
        bool includePermissions,
        CancellationToken cancellationToken);

    Task<bool> NameExistsAsync(string name, Guid? excludeId, CancellationToken cancellationToken);

    void Add(Role role);

    void Remove(Role role);
}
