using CrmSuite.Modules.Catalog.Domain.Entities;

namespace CrmSuite.Modules.Catalog.Application.Abstractions;

public interface IServiceRepository
{
    Task<Service?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<Service?> GetByIdWithPricesAsync(Guid id, CancellationToken cancellationToken);

    Task<Service?> GetByIdWithDetailsAsync(Guid id, CancellationToken cancellationToken);

    Task<bool> CodeExistsAsync(string code, Guid? excludeId, CancellationToken cancellationToken);

    Task<bool> CategoryHasServicesAsync(Guid categoryId, CancellationToken cancellationToken);

    Task<ServicePrice?> GetEffectivePriceAsync(Guid serviceId, DateTime utcDate, CancellationToken cancellationToken);

    void Add(Service service);
}
