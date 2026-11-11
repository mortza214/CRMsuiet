using CrmSuite.BuildingBlocks.Domain.Results;
using CrmSuite.Modules.Orders.Application.Dtos;
using CrmSuite.Modules.Orders.Application.Queries;

namespace CrmSuite.Modules.Orders.Application.Abstractions;

public interface IOrderReadRepository
{
    Task<PagedResult<OrderListItemDto>> SearchAsync(
        OrderSearchFilter filter,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<OrderListItemDto>> GetCustomerOrdersAsync(
        Guid customerId,
        CancellationToken cancellationToken);
}
