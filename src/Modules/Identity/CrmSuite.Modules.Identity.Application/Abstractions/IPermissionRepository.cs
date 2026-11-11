using CrmSuite.Modules.Identity.Domain.Entities;

namespace CrmSuite.Modules.Identity.Application.Abstractions;

public interface IPermissionRepository
{
    Task<IReadOnlyList<Permission>> GetAllAsync(CancellationToken cancellationToken);

    Task<IReadOnlyList<Permission>> GetByIdsAsync(
        IReadOnlyCollection<Guid> ids,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<Permission>> GetByCodesAsync(
        IReadOnlyCollection<string> codes,
        CancellationToken cancellationToken);

    Task AddRangeAsync(IEnumerable<Permission> permissions, CancellationToken cancellationToken);
}
