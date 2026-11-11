using CrmSuite.BuildingBlocks.Domain.Results;
using CrmSuite.Modules.Catalog.Application.Dtos;
using CrmSuite.Modules.Catalog.Application.Queries;

namespace CrmSuite.Modules.Catalog.Application.Abstractions;

public interface ICatalogReadRepository
{
    Task<PagedResult<ServiceListItemDto>> SearchServicesAsync(
        ServiceSearchFilter filter,
        CancellationToken cancellationToken);
}
