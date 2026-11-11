using CrmSuite.Modules.Catalog.Domain.Entities;

namespace CrmSuite.Modules.Catalog.Application.Abstractions;

public interface IDataCenterItemRepository
{
    Task<IReadOnlyList<DataCenterItem>> GetAllAsync(CancellationToken cancellationToken);

    Task<DataCenterItem?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<bool> NameExistsAsync(string name, Guid? excludeId, CancellationToken cancellationToken);

    void Add(DataCenterItem item);

    void Remove(DataCenterItem item);
}
