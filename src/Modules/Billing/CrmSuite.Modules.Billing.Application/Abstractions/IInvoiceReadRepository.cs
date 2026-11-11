using CrmSuite.BuildingBlocks.Domain.Results;
using CrmSuite.Modules.Billing.Application.Dtos;
using CrmSuite.Modules.Billing.Application.Queries;

namespace CrmSuite.Modules.Billing.Application.Abstractions;

public interface IInvoiceReadRepository
{
    Task<PagedResult<InvoiceListItemDto>> SearchAsync(
        InvoiceSearchFilter filter,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<InvoiceListItemDto>> GetCustomerInvoicesAsync(
        Guid customerId,
        CancellationToken cancellationToken);
}
