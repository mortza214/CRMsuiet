using CrmSuite.Modules.Catalog.Domain.Entities;

namespace CrmSuite.Modules.Catalog.Application.Abstractions;

public interface IAddonRepository
{
    Task<IReadOnlyList<ServiceAddon>> GetAllAsync(CancellationToken cancellationToken);

    Task<IReadOnlyList<ServiceAddon>> GetAvailableForServiceAsync(
        Guid serviceId,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<ServiceAddon>> GetByIdsAsync(
        IReadOnlyCollection<Guid> ids,
        CancellationToken cancellationToken);

    Task<ServiceAddon?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<bool> NameExistsAsync(string name, Guid? excludeId, CancellationToken cancellationToken);

    void Add(ServiceAddon addon);

    void Remove(ServiceAddon addon);
}
