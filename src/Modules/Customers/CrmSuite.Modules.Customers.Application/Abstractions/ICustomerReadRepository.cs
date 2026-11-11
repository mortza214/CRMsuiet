using CrmSuite.BuildingBlocks.Domain.Results;
using CrmSuite.Modules.Customers.Application.Dtos;
using CrmSuite.Modules.Customers.Application.Queries;

namespace CrmSuite.Modules.Customers.Application.Abstractions;

public interface ICustomerReadRepository
{
    Task<PagedResult<CustomerListItemDto>> SearchAsync(
        CustomerSearchFilter filter,
        CancellationToken cancellationToken);
}
