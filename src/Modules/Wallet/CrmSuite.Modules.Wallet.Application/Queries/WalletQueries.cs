using CrmSuite.BuildingBlocks.Domain.Results;
using CrmSuite.Modules.Wallet.Application.Dtos;
using CrmSuite.Modules.Wallet.Domain.Enums;
using MediatR;

namespace CrmSuite.Modules.Wallet.Application.Queries;

public sealed record GetWalletBalanceQuery : IRequest<Result<WalletDto>>;

public sealed record GetCustomerWalletBalanceQuery(Guid CustomerId) : IRequest<Result<WalletDto>>;

public sealed record GetWalletTransactionsQuery : IRequest<Result<IReadOnlyList<WalletTransactionDto>>>;

public sealed record GetEmployeeCommissionsQuery(
    CommissionStatus? Status = null) : IRequest<Result<IReadOnlyList<CommissionEntryDto>>>;

public sealed record GetAllCommissionsQuery(
    int Page = 1,
    int PageSize = 20,
    CommissionStatus? Status = null,
    Guid? EmployeeId = null,
    DateTime? FromUtc = null,
    DateTime? ToUtc = null) : IRequest<Result<PagedResult<CommissionEntryDto>>>;
