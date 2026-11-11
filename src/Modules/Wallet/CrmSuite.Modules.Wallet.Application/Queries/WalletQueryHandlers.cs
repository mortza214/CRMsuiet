using CrmSuite.BuildingBlocks.Domain.Results;
using CrmSuite.Modules.Wallet.Application.Abstractions;
using CrmSuite.Modules.Wallet.Application.Dtos;
using CrmSuite.Modules.Wallet.Application.Mapping;
using CrmSuite.Modules.Wallet.Application.Services;
using CrmSuite.Modules.Wallet.Domain.Enums;
using MediatR;
using FoundationCurrentUser = CrmSuite.Modules.Foundation.Application.Abstractions.ICurrentUserService;

namespace CrmSuite.Modules.Wallet.Application.Queries;

public sealed class GetWalletBalanceQueryHandler
    : IRequestHandler<GetWalletBalanceQuery, Result<WalletDto>>
{
    private readonly WalletOwnerResolver _ownerResolver;
    private readonly IWalletRepository _wallets;
    private readonly WalletDtoMapper _mapper;

    public GetWalletBalanceQueryHandler(
        WalletOwnerResolver ownerResolver,
        IWalletRepository wallets,
        WalletDtoMapper mapper)
    {
        _ownerResolver = ownerResolver;
        _wallets = wallets;
        _mapper = mapper;
    }

    public async Task<Result<WalletDto>> Handle(
        GetWalletBalanceQuery request,
        CancellationToken cancellationToken)
    {
        var owner = await _ownerResolver.ResolveAsync(cancellationToken);
        if (owner is null)
        {
            return Result.Failure<WalletDto>("کاربر جاری قابل تشخیص نیست.", "WALLET_CURRENT_USER_MISSING");
        }

        var wallet = await _wallets.GetByOwnerAsync(owner.OwnerId, owner.OwnerType, cancellationToken);

        return Result.Success(wallet is null
            ? new WalletDto(Guid.Empty, owner.OwnerId, owner.OwnerType.ToString(), 0m)
            : _mapper.ToWalletDto(wallet));
    }
}

public sealed class GetCustomerWalletBalanceQueryHandler
    : IRequestHandler<GetCustomerWalletBalanceQuery, Result<WalletDto>>
{
    private readonly IWalletRepository _wallets;
    private readonly WalletDtoMapper _mapper;

    public GetCustomerWalletBalanceQueryHandler(
        IWalletRepository wallets,
        WalletDtoMapper mapper)
    {
        _wallets = wallets;
        _mapper = mapper;
    }

    public async Task<Result<WalletDto>> Handle(
        GetCustomerWalletBalanceQuery request,
        CancellationToken cancellationToken)
    {
        var wallet = await _wallets.GetByOwnerAsync(
            request.CustomerId,
            WalletOwnerType.Customer,
            cancellationToken);

        return Result.Success(wallet is null
            ? new WalletDto(Guid.Empty, request.CustomerId, "Customer", 0m)
            : _mapper.ToWalletDto(wallet));
    }
}

public sealed class GetWalletTransactionsQueryHandler
    : IRequestHandler<GetWalletTransactionsQuery, Result<IReadOnlyList<WalletTransactionDto>>>
{
    private readonly WalletOwnerResolver _ownerResolver;
    private readonly IWalletRepository _wallets;
    private readonly IWalletReadRepository _readRepository;

    public GetWalletTransactionsQueryHandler(
        WalletOwnerResolver ownerResolver,
        IWalletRepository wallets,
        IWalletReadRepository readRepository)
    {
        _ownerResolver = ownerResolver;
        _wallets = wallets;
        _readRepository = readRepository;
    }

    public async Task<Result<IReadOnlyList<WalletTransactionDto>>> Handle(
        GetWalletTransactionsQuery request,
        CancellationToken cancellationToken)
    {
        var owner = await _ownerResolver.ResolveAsync(cancellationToken);
        if (owner is null)
        {
            return Result.Failure<IReadOnlyList<WalletTransactionDto>>(
                "کاربر جاری قابل تشخیص نیست.",
                "WALLET_CURRENT_USER_MISSING");
        }

        var wallet = await _wallets.GetByOwnerAsync(owner.OwnerId, owner.OwnerType, cancellationToken);
        if (wallet is null)
        {
            return Result.Success<IReadOnlyList<WalletTransactionDto>>([]);
        }

        var transactions = await _readRepository.GetTransactionsAsync(wallet.Id, cancellationToken);
        return Result.Success(transactions);
    }
}

public sealed class GetEmployeeCommissionsQueryHandler
    : IRequestHandler<GetEmployeeCommissionsQuery, Result<IReadOnlyList<CommissionEntryDto>>>
{
    private readonly IWalletReadRepository _readRepository;
    private readonly FoundationCurrentUser _currentUser;

    public GetEmployeeCommissionsQueryHandler(
        IWalletReadRepository readRepository,
        FoundationCurrentUser currentUser)
    {
        _readRepository = readRepository;
        _currentUser = currentUser;
    }

    public async Task<Result<IReadOnlyList<CommissionEntryDto>>> Handle(
        GetEmployeeCommissionsQuery request,
        CancellationToken cancellationToken)
    {
        if (_currentUser.UserId is null)
        {
            return Result.Failure<IReadOnlyList<CommissionEntryDto>>(
                "کاربر جاری قابل تشخیص نیست.",
                "WALLET_CURRENT_USER_MISSING");
        }

        var page = await _readRepository.GetCommissionsAsync(
            new CommissionSearchFilter(request.Status, _currentUser.UserId.Value, null, null, 1, 200),
            cancellationToken);

        return Result.Success(page.Items);
    }
}

public sealed class GetAllCommissionsQueryHandler
    : IRequestHandler<GetAllCommissionsQuery, Result<PagedResult<CommissionEntryDto>>>
{
    private readonly IWalletReadRepository _readRepository;

    public GetAllCommissionsQueryHandler(IWalletReadRepository readRepository)
    {
        _readRepository = readRepository;
    }

    public async Task<Result<PagedResult<CommissionEntryDto>>> Handle(
        GetAllCommissionsQuery request,
        CancellationToken cancellationToken)
    {
        var page = await _readRepository.GetCommissionsAsync(
            new CommissionSearchFilter(
                request.Status,
                request.EmployeeId,
                request.FromUtc,
                request.ToUtc,
                request.Page,
                request.PageSize),
            cancellationToken);

        return Result.Success(page);
    }
}
