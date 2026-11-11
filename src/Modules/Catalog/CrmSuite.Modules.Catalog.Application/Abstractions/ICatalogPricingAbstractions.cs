using CrmSuite.Modules.Catalog.Domain.Entities;

namespace CrmSuite.Modules.Catalog.Application.Abstractions;

public interface IServiceUsageGuard
{
    Task<bool> HasActiveOrderItemsForServiceAsync(
        Guid serviceId,
        CancellationToken cancellationToken);

    Task<bool> HasActiveOrderItemsUsingAddonAsync(
        Guid addonId,
        CancellationToken cancellationToken);
}

public interface IServiceFormLinkRepository
{
    Task<IReadOnlyList<ServiceFormLink>> GetByServiceAsync(
        Guid serviceId,
        CancellationToken cancellationToken);

    Task<ServiceFormLink?> GetAsync(
        Guid serviceId,
        Guid formDefinitionId,
        CancellationToken cancellationToken);

    void Add(ServiceFormLink link);

    void Remove(ServiceFormLink link);
}
