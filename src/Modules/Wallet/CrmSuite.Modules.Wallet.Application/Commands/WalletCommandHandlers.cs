using CrmSuite.BuildingBlocks.Application.Abstractions;
using CrmSuite.BuildingBlocks.Domain.Exceptions;
using CrmSuite.BuildingBlocks.Domain.Results;
using CrmSuite.Modules.Billing.Application.Abstractions;
using CrmSuite.Modules.Billing.Domain.Enums;
using CrmSuite.Modules.Catalog.Application.Abstractions;
using CrmSuite.Modules.Customers.Application.Abstractions;
using CrmSuite.Modules.Foundation.Application.Abstractions;
using CrmSuite.Modules.Orders.Application.Abstractions;
using CrmSuite.Modules.Orders.Application.Queries;
using CrmSuite.Modules.Orders.Domain.Enums;
using CrmSuite.Modules.Payments.Application.Abstractions;
using CrmSuite.Modules.Payments.Domain.Entities;
using CrmSuite.Modules.Payments.Domain.Enums;
using CrmSuite.Modules.Wallet.Application.Abstractions;
using CrmSuite.Modules.Wallet.Application.Dtos;
using CrmSuite.Modules.Wallet.Application.Mapping;
using CrmSuite.Modules.Wallet.Application.Services;
using CrmSuite.Modules.Wallet.Domain.Entities;
using CrmSuite.Modules.Wallet.Domain.Enums;
using MediatR;
using FoundationCurrentUser = CrmSuite.Modules.Foundation.Application.Abstractions.ICurrentUserService;

namespace CrmSuite.Modules.Wallet.Application.Commands;

public sealed class ChargeCustomerWalletCommandHandler
    : IRequestHandler<ChargeCustomerWalletCommand, Result<WalletDto>>
{
    private readonly IWalletRepository _wallets;
    private readonly WalletAccountService _walletAccounts;
    private readonly ICustomerRepository _customers;
    private readonly ITransactionRepository _payments;
    private readonly IWalletUnitOfWork _unitOfWork;
    private readonly IAuditScope _auditScope;
    private readonly FoundationCurrentUser _currentUser;
    private readonly IDateTimeProvider _clock;
    private readonly WalletDtoMapper _mapper;

    public ChargeCustomerWalletCommandHandler(
        IWalletRepository wallets,
        WalletAccountService walletAccounts,
        ICustomerRepository customers,
        ITransactionRepository payments,
        IWalletUnitOfWork unitOfWork,
        IAuditScope auditScope,
        FoundationCurrentUser currentUser,
        IDateTimeProvider clock,
        WalletDtoMapper mapper)
    {
        _wallets = wallets;
        _walletAccounts = walletAccounts;
        _customers = customers;
        _payments = payments;
        _unitOfWork = unitOfWork;
        _auditScope = auditScope;
        _currentUser = currentUser;
        _clock = clock;
        _mapper = mapper;
    }

    public async Task<Result<WalletDto>> Handle(
        ChargeCustomerWalletCommand request,
        CancellationToken cancellationToken)
    {
        var customer = await _customers.GetByIdAsync(request.CustomerId, false, cancellationToken);
        if (customer is null)
        {
            return Result.Failure<WalletDto>("مشتری یافت نشد.", "WALLET_CUSTOMER_NOT_FOUND");
        }

        var now = _clock.UtcNow;
        var wallet = await _walletAccounts.GetOrCreateAsync(
            customer.Id,
            WalletOwnerType.Customer,
            _currentUser.UserId,
            cancellationToken);

        var payment = new Transaction(
            null,
            customer.Id,
            request.Amount,
            request.PaymentMethod,
            PaymentGatewayName.None,
            request.ReferenceNumber,
            GenerateTrackingCode(),
            _currentUser.UserId,
            now);

        payment.MarkSuccess(request.ReferenceNumber, "wallet-charge", _currentUser.UserId, now);
        _payments.Add(payment);

        wallet.Deposit(
            request.Amount,
            WalletTransactionType.Deposit,
            payment.Id,
            "شارژ کیف پول مشتری",
            _currentUser.UserId,
            now);

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        _auditScope.EntityId = wallet.Id.ToString();

        return Result.Success(_mapper.ToWalletDto(wallet));
    }

    internal static string GenerateTrackingCode() =>
        $"WLT-{Guid.NewGuid():N}"[..16].ToUpperInvariant();
}

public sealed class PayInvoiceFromWalletCommandHandler
    : IRequestHandler<PayInvoiceFromWalletCommand, Result<WalletDto>>
{
    private readonly WalletOwnerResolver _ownerResolver;
    private readonly IWalletRepository _wallets;
    private readonly IInvoiceRepository _invoices;
    private readonly IOrderRepository _orders;
    private readonly ITransactionRepository _payments;
    private readonly IWalletUnitOfWork _unitOfWork;
    private readonly IAuditScope _auditScope;
    private readonly FoundationCurrentUser _currentUser;
    private readonly IDateTimeProvider _clock;
    private readonly WalletDtoMapper _mapper;

    public PayInvoiceFromWalletCommandHandler(
        WalletOwnerResolver ownerResolver,
        IWalletRepository wallets,
        IInvoiceRepository invoices,
        IOrderRepository orders,
        ITransactionRepository payments,
        IWalletUnitOfWork unitOfWork,
        IAuditScope auditScope,
        FoundationCurrentUser currentUser,
        IDateTimeProvider clock,
        WalletDtoMapper mapper)
    {
        _ownerResolver = ownerResolver;
        _wallets = wallets;
        _invoices = invoices;
        _orders = orders;
        _payments = payments;
        _unitOfWork = unitOfWork;
        _auditScope = auditScope;
        _currentUser = currentUser;
        _clock = clock;
        _mapper = mapper;
    }

    public async Task<Result<WalletDto>> Handle(
        PayInvoiceFromWalletCommand request,
        CancellationToken cancellationToken)
    {
        var owner = await _ownerResolver.ResolveAsync(cancellationToken);
        if (owner is null)
        {
            return Result.Failure<WalletDto>("کاربر جاری قابل تشخیص نیست.", "WALLET_CURRENT_USER_MISSING");
        }

        Guid customerId;

        if (owner.OwnerType == WalletOwnerType.Customer)
        {
            if (request.CustomerId.HasValue && request.CustomerId.Value != owner.OwnerId)
            {
                return Result.Failure<WalletDto>("این فاکتور به مشتری جاری تعلق ندارد.", "WALLET_INVOICE_FORBIDDEN");
            }

            customerId = owner.OwnerId;
        }
        else
        {
            if (!request.CustomerId.HasValue)
            {
                return Result.Failure<WalletDto>(
                    "برای پرداخت از کیف پول مشتری، شناسه مشتری الزامی است.",
                    "WALLET_CUSTOMER_REQUIRED");
            }

            customerId = request.CustomerId.Value;
        }

        var invoice = await _invoices.GetByIdWithItemsAsync(request.InvoiceId, cancellationToken);
        if (invoice is null || invoice.CustomerId != customerId)
        {
            return Result.Failure<WalletDto>("فاکتور یافت نشد.", "WALLET_INVOICE_NOT_FOUND");
        }

        if (invoice.Status is not (InvoiceStatus.Issued or InvoiceStatus.PartiallyPaid))
        {
            return Result.Failure<WalletDto>("فاکتور در وضعیت قابل پرداخت نیست.", "WALLET_INVOICE_STATUS_INVALID");
        }

        var wallet = await _wallets.GetByOwnerAsync(
            invoice.CustomerId,
            WalletOwnerType.Customer,
            cancellationToken);

        if (wallet is null || wallet.Balance <= 0)
        {
            return Result.Failure<WalletDto>("کیف پول مشتری موجودی ندارد.", "WALLET_INSUFFICIENT_BALANCE");
        }

        var remaining = invoice.TotalAmount - invoice.PaidAmount;
        if (wallet.Balance < remaining)
        {
            return Result.Failure<WalletDto>("موجودی کیف پول برای پرداخت کامل فاکتور کافی نیست.", "WALLET_INSUFFICIENT_BALANCE");
        }

        var now = _clock.UtcNow;
        var order = await _orders.GetByIdWithDetailsAsync(invoice.OrderId, cancellationToken);
        if (order is null)
        {
            return Result.Failure<WalletDto>("سفارش فاکتور یافت نشد.", "WALLET_ORDER_NOT_FOUND");
        }

        if (order.Status is OrderStatus.Draft or OrderStatus.Canceled)
        {
            return Result.Failure<WalletDto>("سفارش در وضعیت قابل پرداخت نیست.", "WALLET_ORDER_STATUS_INVALID");
        }

        try
        {
            var walletTransaction = wallet.Withdraw(
                remaining,
                WalletTransactionType.InvoicePayment,
                invoice.Id,
                $"پرداخت فاکتور {invoice.InvoiceNumber} از کیف پول",
                _currentUser.UserId,
                now);

            invoice.RegisterPayment(remaining, _currentUser.UserId, now);

            if (invoice.Status == InvoiceStatus.Paid && order.Status == OrderStatus.PendingPayment)
            {
                order.ChangeStatus(
                    OrderStatus.Paid,
                    _currentUser.UserId,
                    now,
                    $"پرداخت فاکتور {invoice.InvoiceNumber} از کیف پول");
            }

            var payment = new Transaction(
                invoice.Id,
                invoice.CustomerId,
                remaining,
                PaymentMethod.Wallet,
                PaymentGatewayName.None,
                invoice.InvoiceNumber,
                GenerateManualPaymentTrackingCode(),
                _currentUser.UserId,
                now);

            payment.MarkSuccess(invoice.InvoiceNumber, "wallet-invoice-payment", _currentUser.UserId, now);
            _payments.Add(payment);

            _ = walletTransaction;
        }
        catch (DomainException exception)
        {
            return Result.Failure<WalletDto>(exception.Message, exception.Code);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        _auditScope.EntityId = wallet.Id.ToString();

        return Result.Success(_mapper.ToWalletDto(wallet));
    }

    private static string GenerateManualPaymentTrackingCode() =>
        $"WLT-{Guid.NewGuid():N}"[..16].ToUpperInvariant();
}

public sealed class RequestCommissionWithdrawCommandHandler
    : IRequestHandler<RequestCommissionWithdrawCommand, Result<WithdrawalDto>>
{
    private readonly WalletOwnerResolver _ownerResolver;
    private readonly IWalletRepository _wallets;
    private readonly IWithdrawalRepository _withdrawals;
    private readonly IWalletUnitOfWork _unitOfWork;
    private readonly FoundationCurrentUser _currentUser;
    private readonly IDateTimeProvider _clock;
    private readonly WalletDtoMapper _mapper;

    public RequestCommissionWithdrawCommandHandler(
        WalletOwnerResolver ownerResolver,
        IWalletRepository wallets,
        IWithdrawalRepository withdrawals,
        IWalletUnitOfWork unitOfWork,
        FoundationCurrentUser currentUser,
        IDateTimeProvider clock,
        WalletDtoMapper mapper)
    {
        _ownerResolver = ownerResolver;
        _wallets = wallets;
        _withdrawals = withdrawals;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
        _clock = clock;
        _mapper = mapper;
    }

    public async Task<Result<WithdrawalDto>> Handle(
        RequestCommissionWithdrawCommand request,
        CancellationToken cancellationToken)
    {
        var owner = await _ownerResolver.ResolveAsync(cancellationToken);
        if (owner is null || owner.OwnerType != WalletOwnerType.Employee)
        {
            return Result.Failure<WithdrawalDto>("درخواست برداشت فقط برای کارکنان مجاز است.", "WALLET_EMPLOYEE_ONLY");
        }

        var wallet = await _wallets.GetByOwnerAsync(
            owner.OwnerId,
            WalletOwnerType.Employee,
            cancellationToken);

        if (wallet is null || wallet.Balance < request.Amount)
        {
            return Result.Failure<WithdrawalDto>("موجودی کیف پول کافی نیست.", "WALLET_INSUFFICIENT_BALANCE");
        }

        var withdrawal = new WithdrawalRequest(
            wallet.Id,
            owner.OwnerId,
            request.Amount,
            _currentUser.UserId,
            _clock.UtcNow);

        _withdrawals.Add(withdrawal);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(_mapper.ToWithdrawalDto(withdrawal));
    }
}

public sealed class ApproveCommissionWithdrawCommandHandler
    : IRequestHandler<ApproveCommissionWithdrawCommand, Result<WithdrawalDto>>
{
    private readonly IWithdrawalRepository _withdrawals;
    private readonly IWalletRepository _wallets;
    private readonly IWalletUnitOfWork _unitOfWork;
    private readonly IAuditScope _auditScope;
    private readonly FoundationCurrentUser _currentUser;
    private readonly IDateTimeProvider _clock;
    private readonly WalletDtoMapper _mapper;

    public ApproveCommissionWithdrawCommandHandler(
        IWithdrawalRepository withdrawals,
        IWalletRepository wallets,
        IWalletUnitOfWork unitOfWork,
        IAuditScope auditScope,
        FoundationCurrentUser currentUser,
        IDateTimeProvider clock,
        WalletDtoMapper mapper)
    {
        _withdrawals = withdrawals;
        _wallets = wallets;
        _unitOfWork = unitOfWork;
        _auditScope = auditScope;
        _currentUser = currentUser;
        _clock = clock;
        _mapper = mapper;
    }

    public async Task<Result<WithdrawalDto>> Handle(
        ApproveCommissionWithdrawCommand request,
        CancellationToken cancellationToken)
    {
        var withdrawal = await _withdrawals.GetByIdAsync(request.WithdrawalId, cancellationToken);
        if (withdrawal is null)
        {
            return Result.Failure<WithdrawalDto>("درخواست برداشت یافت نشد.", "WALLET_WITHDRAW_NOT_FOUND");
        }

        var wallet = await _wallets.GetByIdAsync(withdrawal.WalletId, cancellationToken);
        if (wallet is null)
        {
            return Result.Failure<WithdrawalDto>("کیف پول یافت نشد.", "WALLET_NOT_FOUND");
        }

        try
        {
            wallet.Withdraw(
                withdrawal.Amount,
                WalletTransactionType.Withdraw,
                withdrawal.Id,
                "برداشت پورسانت",
                _currentUser.UserId,
                _clock.UtcNow);

            withdrawal.Approve(_currentUser.UserId, _clock.UtcNow, request.Note);
        }
        catch (DomainException exception)
        {
            return Result.Failure<WithdrawalDto>(exception.Message, exception.Code);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        _auditScope.EntityId = withdrawal.Id.ToString();

        return Result.Success(_mapper.ToWithdrawalDto(withdrawal));
    }
}

public sealed class ApproveCommissionCommandHandler
    : IRequestHandler<ApproveCommissionCommand, Result<CommissionEntryDto>>
{
    private readonly ICommissionRepository _commissions;
    private readonly WalletAccountService _walletAccounts;
    private readonly IWalletUnitOfWork _unitOfWork;
    private readonly IAuditScope _auditScope;
    private readonly FoundationCurrentUser _currentUser;
    private readonly IDateTimeProvider _clock;
    private readonly WalletDtoMapper _mapper;

    public ApproveCommissionCommandHandler(
        ICommissionRepository commissions,
        WalletAccountService walletAccounts,
        IWalletUnitOfWork unitOfWork,
        IAuditScope auditScope,
        FoundationCurrentUser currentUser,
        IDateTimeProvider clock,
        WalletDtoMapper mapper)
    {
        _commissions = commissions;
        _walletAccounts = walletAccounts;
        _unitOfWork = unitOfWork;
        _auditScope = auditScope;
        _currentUser = currentUser;
        _clock = clock;
        _mapper = mapper;
    }

    public async Task<Result<CommissionEntryDto>> Handle(
        ApproveCommissionCommand request,
        CancellationToken cancellationToken)
    {
        var entry = await _commissions.GetByIdWithSchedulesAsync(request.CommissionId, cancellationToken);
        if (entry is null)
        {
            return Result.Failure<CommissionEntryDto>("پورسانت یافت نشد.", "WALLET_COMMISSION_NOT_FOUND");
        }

        try
        {
            var now = _clock.UtcNow;
            entry.Approve(_currentUser.UserId, now);

            foreach (var share in entry.Shares.Where(share => !share.IsCompanyShare && share.UserId.HasValue))
            {
                var wallet = await _walletAccounts.GetOrCreateAsync(
                    share.UserId!.Value,
                    WalletOwnerType.Employee,
                    _currentUser.UserId,
                    cancellationToken);
                wallet.Deposit(
                    share.Amount,
                    WalletTransactionType.CommissionPayout,
                    entry.Id,
                    $"پورسانت ماه اول - {entry.CustomerName ?? entry.OrderId.ToString()}",
                    _currentUser.UserId,
                    now);
            }

            entry.MarkInstallmentPaid(now);
        }
        catch (DomainException exception)
        {
            return Result.Failure<CommissionEntryDto>(exception.Message, exception.Code);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        _auditScope.EntityId = entry.Id.ToString();

        return Result.Success(_mapper.ToCommissionDto(entry, null, null));
    }
}

public sealed class RejectCommissionCommandHandler
    : IRequestHandler<RejectCommissionCommand, Result<CommissionEntryDto>>
{
    private readonly ICommissionRepository _commissions;
    private readonly IWalletUnitOfWork _unitOfWork;
    private readonly IAuditScope _auditScope;
    private readonly FoundationCurrentUser _currentUser;
    private readonly IDateTimeProvider _clock;
    private readonly WalletDtoMapper _mapper;

    public RejectCommissionCommandHandler(
        ICommissionRepository commissions,
        IWalletUnitOfWork unitOfWork,
        IAuditScope auditScope,
        FoundationCurrentUser currentUser,
        IDateTimeProvider clock,
        WalletDtoMapper mapper)
    {
        _commissions = commissions;
        _unitOfWork = unitOfWork;
        _auditScope = auditScope;
        _currentUser = currentUser;
        _clock = clock;
        _mapper = mapper;
    }

    public async Task<Result<CommissionEntryDto>> Handle(
        RejectCommissionCommand request,
        CancellationToken cancellationToken)
    {
        var entry = await _commissions.GetByIdWithSchedulesAsync(
            request.CommissionId,
            cancellationToken);

        if (entry is null)
        {
            return Result.Failure<CommissionEntryDto>(
                "پورسانت یافت نشد.",
                "WALLET_COMMISSION_NOT_FOUND");
        }

        try
        {
            entry.Reject(_currentUser.UserId, _clock.UtcNow);
        }
        catch (DomainException exception)
        {
            return Result.Failure<CommissionEntryDto>(exception.Message, exception.Code);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        _auditScope.EntityId = entry.Id.ToString();

        return Result.Success(_mapper.ToCommissionDto(entry, null, null));
    }
}

public sealed class UpdateCommissionSharesCommandHandler
    : IRequestHandler<UpdateCommissionSharesCommand, Result<CommissionEntryDto>>
{
    private readonly ICommissionRepository _commissions;
    private readonly IWalletUnitOfWork _unitOfWork;
    private readonly IAuditScope _auditScope;
    private readonly FoundationCurrentUser _currentUser;
    private readonly IDateTimeProvider _clock;
    private readonly WalletDtoMapper _mapper;

    public UpdateCommissionSharesCommandHandler(
        ICommissionRepository commissions,
        IWalletUnitOfWork unitOfWork,
        IAuditScope auditScope,
        FoundationCurrentUser currentUser,
        IDateTimeProvider clock,
        WalletDtoMapper mapper)
    {
        _commissions = commissions;
        _unitOfWork = unitOfWork;
        _auditScope = auditScope;
        _currentUser = currentUser;
        _clock = clock;
        _mapper = mapper;
    }

    public async Task<Result<CommissionEntryDto>> Handle(
        UpdateCommissionSharesCommand request,
        CancellationToken cancellationToken)
    {
        var entry = await _commissions.GetByIdWithSchedulesAsync(request.CommissionId, cancellationToken);
        if (entry is null)
        {
            return Result.Failure<CommissionEntryDto>("پورسانت یافت نشد.", "WALLET_COMMISSION_NOT_FOUND");
        }

        try
        {
            var shares = request.Shares
                .Select(item => new CommissionShare(
                    entry.Id,
                    item.UserId,
                    item.Percentage,
                    decimal.Round(
                        entry.Amount * item.Percentage / 100m,
                        2,
                        MidpointRounding.AwayFromZero),
                    item.IsCompanyShare))
                .ToArray();
            entry.ReplaceShares(shares);
        }
        catch (DomainException exception)
        {
            return Result.Failure<CommissionEntryDto>(exception.Message, exception.Code);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        _auditScope.EntityId = entry.Id.ToString();
        return Result.Success(_mapper.ToCommissionDto(entry, null, null));
    }
}

public sealed class CreateCommissionRuleCommandHandler
    : IRequestHandler<CreateCommissionRuleCommand, Result<CommissionRuleDto>>
{
    private readonly ICommissionRuleRepository _rules;
    private readonly IWalletUnitOfWork _unitOfWork;
    private readonly IAuditScope _auditScope;
    private readonly FoundationCurrentUser _currentUser;
    private readonly IDateTimeProvider _clock;

    public CreateCommissionRuleCommandHandler(
        ICommissionRuleRepository rules,
        IWalletUnitOfWork unitOfWork,
        IAuditScope auditScope,
        FoundationCurrentUser currentUser,
        IDateTimeProvider clock)
    {
        _rules = rules;
        _unitOfWork = unitOfWork;
        _auditScope = auditScope;
        _currentUser = currentUser;
        _clock = clock;
    }

    public async Task<Result<CommissionRuleDto>> Handle(
        CreateCommissionRuleCommand request,
        CancellationToken cancellationToken)
    {
        CommissionRule rule;

        try
        {
            rule = new CommissionRule(
                request.ServiceId,
                request.ServiceType,
                request.Percentage,
                request.FixedAmount,
                request.PayoutType,
                _currentUser.UserId,
                _clock.UtcNow);
        }
        catch (DomainException exception)
        {
            return Result.Failure<CommissionRuleDto>(exception.Message, exception.Code);
        }

        _rules.Add(rule);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        _auditScope.EntityId = rule.Id.ToString();

        return Result.Success(new CommissionRuleDto(
            rule.Id,
            rule.ServiceId,
            rule.ServiceType?.ToString(),
            rule.Percentage,
            rule.FixedAmount,
            rule.PayoutType.ToString()));
    }
}

public sealed class EvaluateOrderCommissionCommandHandler
    : IRequestHandler<EvaluateOrderCommissionCommand, Result<CommissionEvaluationResultDto>>
{
    private readonly IMediator _mediator;
    private readonly ICustomerRepository _customers;
    private readonly IServiceRepository _services;
    private readonly ICommissionRuleRepository _rules;
    private readonly ICommissionCalculator _calculator;
    private readonly ICommissionRepository _commissions;
    private readonly IWalletUnitOfWork _unitOfWork;
    private readonly FoundationCurrentUser _currentUser;
    private readonly IDateTimeProvider _clock;

    public EvaluateOrderCommissionCommandHandler(
        IMediator mediator,
        ICustomerRepository customers,
        IServiceRepository services,
        ICommissionRuleRepository rules,
        ICommissionCalculator calculator,
        ICommissionRepository commissions,
        IWalletUnitOfWork unitOfWork,
        FoundationCurrentUser currentUser,
        IDateTimeProvider clock)
    {
        _mediator = mediator;
        _customers = customers;
        _services = services;
        _rules = rules;
        _calculator = calculator;
        _commissions = commissions;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
        _clock = clock;
    }

    public async Task<Result<CommissionEvaluationResultDto>> Handle(
        EvaluateOrderCommissionCommand request,
        CancellationToken cancellationToken)
    {
        var orderResult = await _mediator.Send(new GetOrderByIdQuery(request.OrderId), cancellationToken);
        if (orderResult.IsFailure || orderResult.Value is null)
        {
            return Result.Failure<CommissionEvaluationResultDto>(
                orderResult.Errors,
                orderResult.Message ?? "سفارش یافت نشد.");
        }

        var order = orderResult.Value;
        if (order.Status == "Canceled")
        {
            return Result.Failure<CommissionEvaluationResultDto>(
                "برای سفارش لغوشده پورسانت محاسبه نمی‌شود.",
                "WALLET_ORDER_STATUS_INVALID");
        }

        if (await _commissions.ExistsForOrderAsync(order.Id, cancellationToken))
        {
            return Result.Failure<CommissionEvaluationResultDto>(
                "پورسانت این سفارش قبلاً محاسبه شده است.",
                "WALLET_COMMISSION_DUPLICATE");
        }

        var customer = await _customers.GetByIdAsync(order.CustomerId, false, cancellationToken);
        var ownerId = customer?.OwnerUserId;
        var now = _clock.UtcNow;
        var totalCommission = 0m;
        var entryCount = 0;

        foreach (var item in order.Items)
        {
            var service = item.ServiceId.HasValue
                ? await _services.GetByIdAsync(item.ServiceId.Value, cancellationToken)
                : null;

            var rule = await _rules.GetMatchingAsync(item.ServiceId, service?.ServiceType, cancellationToken);
            if (rule is null)
            {
                continue;
            }

            var commission = _calculator.Calculate(rule, item.TotalPrice);
            if (commission <= 0)
            {
                continue;
            }

            var recipients = new List<(Guid EmployeeId, decimal Amount)>();
            if (ownerId.HasValue && ownerId.Value != order.SalesExpertId)
            {
                var half = decimal.Round(commission / 2m, 2, MidpointRounding.AwayFromZero);
                recipients.Add((order.SalesExpertId, half));
                recipients.Add((ownerId.Value, commission - half));
            }
            else
            {
                recipients.Add((order.SalesExpertId, commission));
            }

            foreach (var (employeeId, amount) in recipients)
            {
                var entry = new CommissionEntry(
                    order.Id,
                    item.Id,
                    employeeId,
                    amount,
                    rule.PayoutType,
                    _currentUser.UserId,
                    now);

                AddSchedules(entry, order.OrderDate, now);
                _commissions.Add(entry);
                entryCount++;
                totalCommission += amount;
            }
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(new CommissionEvaluationResultDto(entryCount, totalCommission));
    }

    private static void AddSchedules(CommissionEntry entry, DateTime orderDate, DateTime now)
    {
        var periods = entry.PayoutType == CommissionPayoutType.Monthly ? 12 : 1;
        var installment = decimal.Round(
            entry.Amount / periods,
            2,
            MidpointRounding.AwayFromZero);

        for (var index = 0; index < periods; index++)
        {
            var dueDate = entry.PayoutType == CommissionPayoutType.Monthly
                ? orderDate.AddMonths(index + 1)
                : orderDate;

            var amount = index == periods - 1
                ? entry.Amount - (installment * (periods - 1))
                : installment;

            entry.AddSchedule(new CommissionSchedule(entry.Id, dueDate, amount, now));
        }
    }
}
