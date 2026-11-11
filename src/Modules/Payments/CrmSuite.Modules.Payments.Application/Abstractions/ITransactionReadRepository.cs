using CrmSuite.BuildingBlocks.Domain.Results;
using CrmSuite.Modules.Payments.Application.Dtos;
using CrmSuite.Modules.Payments.Application.Queries;

namespace CrmSuite.Modules.Payments.Application.Abstractions;

public interface ITransactionReadRepository
{
    Task<PagedResult<TransactionListItemDto>> SearchAsync(
        TransactionSearchFilter filter,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<TransactionListItemDto>> GetCustomerTransactionsAsync(
        Guid customerId,
        CancellationToken cancellationToken);
}
