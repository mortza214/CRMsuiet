using CrmSuite.BuildingBlocks.Domain.Results;
using CrmSuite.Modules.Wallet.Application.Dtos;
using CrmSuite.Modules.Wallet.Application.Queries;

namespace CrmSuite.Modules.Wallet.Application.Abstractions;

public interface IWalletReadRepository
{
    Task<IReadOnlyList<WalletTransactionDto>> GetTransactionsAsync(
        Guid walletId,
        CancellationToken cancellationToken);

    Task<PagedResult<CommissionEntryDto>> GetCommissionsAsync(
        CommissionSearchFilter filter,
        CancellationToken cancellationToken);
}
