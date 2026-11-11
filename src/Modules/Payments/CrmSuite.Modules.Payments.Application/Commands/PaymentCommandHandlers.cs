using CrmSuite.BuildingBlocks.Application.Abstractions;
using CrmSuite.BuildingBlocks.Domain.Results;
using CrmSuite.Modules.Billing.Application.Abstractions;
using CrmSuite.Modules.Billing.Domain.Enums;
using CrmSuite.Modules.Customers.Application.Abstractions;
using CrmSuite.Modules.Foundation.Application.Abstractions;
using CrmSuite.Modules.Payments.Application.Abstractions;
using CrmSuite.Modules.Payments.Application.Dtos;
using CrmSuite.Modules.Payments.Application.Mapping;
using CrmSuite.Modules.Payments.Application.Queries;
using CrmSuite.Modules.Payments.Application.Services;
using CrmSuite.Modules.Workflow.Application.Commands;
using CrmSuite.Modules.Payments.Domain.Entities;
using CrmSuite.Modules.Payments.Domain.Enums;
using MediatR;
using Microsoft.Extensions.Logging;
using FoundationCurrentUser = CrmSuite.Modules.Foundation.Application.Abstractions.ICurrentUserService;

namespace CrmSuite.Modules.Payments.Application.Commands;

public sealed class CreateManualPaymentCommandHandler
    : IRequestHandler<CreateManualPaymentCommand, Result<TransactionDto>>
{
    private readonly ITransactionRepository _transactions;
    private readonly IInvoiceRepository _invoices;
    private readonly ICustomerRepository _customers;
    private readonly IPaymentUnitOfWork _unitOfWork;
    private readonly IAuditScope _auditScope;
    private readonly FoundationCurrentUser _currentUser;
    private readonly IDateTimeProvider _clock;
    private readonly PaymentDtoMapper _mapper;
    private readonly IMediator? _mediator;
    private readonly ILogger<CreateManualPaymentCommandHandler>? _logger;

    public CreateManualPaymentCommandHandler(
        ITransactionRepository transactions,
        IInvoiceRepository invoices,
        ICustomerRepository customers,
        IPaymentUnitOfWork unitOfWork,
        IAuditScope auditScope,
        FoundationCurrentUser currentUser,
        IDateTimeProvider clock,
        PaymentDtoMapper mapper,
        IMediator? mediator = null,
        ILogger<CreateManualPaymentCommandHandler>? logger = null)
    {
        _transactions = transactions;
        _invoices = invoices;
        _customers = customers;
        _unitOfWork = unitOfWork;
        _auditScope = auditScope;
        _currentUser = currentUser;
        _clock = clock;
        _mapper = mapper;
        _mediator = mediator;
        _logger = logger;
    }

    public async Task<Result<TransactionDto>> Handle(
        CreateManualPaymentCommand request,
        CancellationToken cancellationToken)
    {
        var customer = await _customers.GetByIdAsync(request.CustomerId, false, cancellationToken);
        if (customer is null)
        {
            return Result.Failure<TransactionDto>("مشتری یافت نشد.", "PAYMENTS_CUSTOMER_NOT_FOUND");
        }

        var now = _clock.UtcNow;
        string? invoiceNumber = null;

        if (request.InvoiceId.HasValue)
        {
            var invoice = await _invoices.GetByIdWithItemsAsync(request.InvoiceId.Value, cancellationToken);
            if (invoice is null)
            {
                return Result.Failure<TransactionDto>("فاکتور یافت نشد.", "PAYMENTS_INVOICE_NOT_FOUND");
            }

            if (invoice.CustomerId != request.CustomerId)
            {
                return Result.Failure<TransactionDto>(
                    "فاکتور به مشتری انتخاب‌شده تعلق ندارد.",
                    "PAYMENTS_INVOICE_CUSTOMER_MISMATCH");
            }

            if (invoice.Status is not (InvoiceStatus.Issued or InvoiceStatus.PartiallyPaid or InvoiceStatus.PendingPayment))
            {
                return Result.Failure<TransactionDto>(
                    "فاکتور در وضعیت قابل پرداخت نیست.",
                    "PAYMENTS_INVOICE_STATUS_INVALID");
            }

            if (request.Amount > invoice.TotalAmount - invoice.PaidAmount)
            {
                return Result.Failure<TransactionDto>(
                    "مبلغ پرداخت بیش از مانده فاکتور است.",
                    "PAYMENTS_AMOUNT_EXCEEDS_REMAINING");
            }

            invoiceNumber = invoice.InvoiceNumber;
            if (!string.IsNullOrWhiteSpace(request.ReceiptImageUrl))
            {
                invoice.SetReceiptImageUrl(request.ReceiptImageUrl, _currentUser.UserId, now);
                if (invoice.Status == InvoiceStatus.PendingPayment)
                {
                    invoice.MarkPendingVerification(_currentUser.UserId, now);
                }
            }
        }

        var transaction = new Transaction(
            request.InvoiceId,
            request.CustomerId,
            request.Amount,
            request.PaymentMethod,
            PaymentGatewayName.None,
            request.ReferenceNumber,
            GenerateTrackingCode(),
            _currentUser.UserId,
            now);

        _transactions.Add(transaction);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        _auditScope.EntityId = transaction.Id.ToString();

        if (_mediator is not null && request.InvoiceId.HasValue)
        {
            try
            {
                var invoice = await _invoices.GetByIdWithItemsAsync(
                    request.InvoiceId.Value,
                    cancellationToken);
                if (invoice is not null)
                {
                    var advance = await _mediator.Send(
                        new AdvanceToFinanceReviewCommand(invoice.OrderId),
                        cancellationToken);
                    if (advance.IsFailure)
                    {
                        _logger?.LogError(
                            "Finance review transition failed for order {OrderId}: {Message}",
                            invoice.OrderId,
                            advance.Message);
                        return Result.Failure<TransactionDto>(
                            "پرداخت ثبت شد، اما انتقال به مرحله بررسی مالی با خطا مواجه شد. لطفاً با پشتیبانی تماس بگیرید.",
                            "PAYMENTS_FINANCE_TRANSITION_FAILED");
                    }
                }
            }
            catch (Exception exception)
            {
                _logger?.LogError(
                    exception,
                    "Finance review transition threw for invoice {InvoiceId}",
                    request.InvoiceId);
                return Result.Failure<TransactionDto>(
                    "پرداخت ثبت شد، اما انتقال به مرحله بررسی مالی با خطا مواجه شد. لطفاً با پشتیبانی تماس بگیرید.",
                    "PAYMENTS_FINANCE_TRANSITION_FAILED");
            }
        }

        return Result.Success(_mapper.ToDto(transaction, invoiceNumber, customer.DisplayName));
    }

    internal static string GenerateTrackingCode() =>
        $"PAY-{Guid.NewGuid():N}"[..16].ToUpperInvariant();
}

public sealed class ApproveManualPaymentCommandHandler
    : IRequestHandler<ApproveManualPaymentCommand, Result<TransactionDto>>
{
    private readonly ITransactionRepository _transactions;
    private readonly IInvoiceRepository _invoices;
    private readonly ICustomerRepository _customers;
    private readonly PaymentSettlementService _settlement;
    private readonly PaymentDtoMapper _mapper;

    public ApproveManualPaymentCommandHandler(
        ITransactionRepository transactions,
        IInvoiceRepository invoices,
        ICustomerRepository customers,
        PaymentSettlementService settlement,
        PaymentDtoMapper mapper)
    {
        _transactions = transactions;
        _invoices = invoices;
        _customers = customers;
        _settlement = settlement;
        _mapper = mapper;
    }

    public async Task<Result<TransactionDto>> Handle(
        ApproveManualPaymentCommand request,
        CancellationToken cancellationToken)
    {
        var transaction = await _transactions.GetByIdAsync(request.TransactionId, cancellationToken);
        if (transaction is null)
        {
            return Result.Failure<TransactionDto>("تراکنش یافت نشد.", "PAYMENTS_TRANSACTION_NOT_FOUND");
        }

        if (transaction.Status != TransactionStatus.Pending)
        {
            return Result.Failure<TransactionDto>(
                "فقط تراکنش در انتظار قابل تأیید است.",
                "PAYMENTS_TRANSACTION_STATUS_INVALID");
        }

        if (transaction.PaymentMethod is not (PaymentMethod.ManualTransfer or PaymentMethod.Cash))
        {
            return Result.Failure<TransactionDto>(
                "این تراکنش پرداخت دستی نیست.",
                "PAYMENTS_NOT_MANUAL");
        }

        var result = await _settlement.SettleAsync(
            transaction,
            transaction.ReferenceNumber ?? "MANUAL-APPROVED",
            "manual-approved",
            cancellationToken);

        if (result.IsFailure)
        {
            return Result.Failure<TransactionDto>(result.Errors, result.Message ?? "تأیید پرداخت ناموفق بود.");
        }

        var customer = await _customers.GetByIdAsync(transaction.CustomerId, false, cancellationToken);
        var invoiceNumber = result.Value?.Invoice?.InvoiceNumber;

        return Result.Success(_mapper.ToDto(transaction, invoiceNumber, customer?.DisplayName));
    }
}

public sealed class InitiateOnlinePaymentCommandHandler
    : IRequestHandler<InitiateOnlinePaymentCommand, Result<OnlinePaymentInitiationDto>>
{
    private readonly IInvoiceRepository _invoices;
    private readonly ITransactionRepository _transactions;
    private readonly IPaymentGatewayTransactionRepository _gatewayTransactions;
    private readonly ICustomerRepository _customers;
    private readonly ISamanGateway _gateway;
    private readonly IPaymentUnitOfWork _unitOfWork;
    private readonly IAuditScope _auditScope;
    private readonly FoundationCurrentUser _currentUser;
    private readonly IDateTimeProvider _clock;

    public InitiateOnlinePaymentCommandHandler(
        IInvoiceRepository invoices,
        ITransactionRepository transactions,
        IPaymentGatewayTransactionRepository gatewayTransactions,
        ICustomerRepository customers,
        ISamanGateway gateway,
        IPaymentUnitOfWork unitOfWork,
        IAuditScope auditScope,
        FoundationCurrentUser currentUser,
        IDateTimeProvider clock)
    {
        _invoices = invoices;
        _transactions = transactions;
        _gatewayTransactions = gatewayTransactions;
        _customers = customers;
        _gateway = gateway;
        _unitOfWork = unitOfWork;
        _auditScope = auditScope;
        _currentUser = currentUser;
        _clock = clock;
    }

    public async Task<Result<OnlinePaymentInitiationDto>> Handle(
        InitiateOnlinePaymentCommand request,
        CancellationToken cancellationToken)
    {
        var now = _clock.UtcNow;
        var invoice = await _invoices.GetByIdWithItemsAsync(request.InvoiceId, cancellationToken);

        if (invoice is null)
        {
            return Result.Failure<OnlinePaymentInitiationDto>("فاکتور یافت نشد.", "PAYMENTS_INVOICE_NOT_FOUND");
        }

        if (_currentUser.UserId is null)
        {
            return Result.Failure<OnlinePaymentInitiationDto>(
                "کاربر جاری قابل تشخیص نیست.",
                "PAYMENTS_CURRENT_USER_MISSING");
        }

        var customer = await _customers.GetByUserIdAsync(
            _currentUser.UserId.Value,
            false,
            cancellationToken);

        if (customer is not null && customer.Id != invoice.CustomerId)
        {
            return Result.Failure<OnlinePaymentInitiationDto>(
                "این فاکتور به مشتری جاری تعلق ندارد.",
                "PAYMENTS_INVOICE_FORBIDDEN");
        }

        if (invoice.Status is not (InvoiceStatus.Issued or InvoiceStatus.PartiallyPaid or InvoiceStatus.PendingPayment))
        {
            return Result.Failure<OnlinePaymentInitiationDto>(
                "فاکتور در وضعیت قابل پرداخت نیست.",
                "PAYMENTS_INVOICE_STATUS_INVALID");
        }

        var remaining = invoice.TotalAmount - invoice.PaidAmount;
        if (remaining <= 0)
        {
            return Result.Failure<OnlinePaymentInitiationDto>(
                "مانده‌ای برای پرداخت وجود ندارد.",
                "PAYMENTS_NOTHING_TO_PAY");
        }

        var callbackUrl = string.IsNullOrWhiteSpace(request.CallbackUrl)
            ? "https://crm.local/payment/callback"
            : request.CallbackUrl;

        var gatewayResult = await _gateway.RequestPaymentAsync(
            remaining,
            invoice.Id,
            callbackUrl,
            cancellationToken);

        if (!gatewayResult.Success || string.IsNullOrWhiteSpace(gatewayResult.Token))
        {
            return Result.Failure<OnlinePaymentInitiationDto>(
                gatewayResult.Error ?? "درگاه پرداخت پاسخ معتبری برنگرداند.",
                "PAYMENTS_GATEWAY_REQUEST_FAILED");
        }

        var trackingCode = CreateManualPaymentCommandHandler.GenerateTrackingCode();
        var transaction = new Transaction(
            invoice.Id,
            invoice.CustomerId,
            remaining,
            PaymentMethod.OnlineGateway,
            PaymentGatewayName.Saman,
            gatewayResult.Token,
            trackingCode,
            _currentUser.UserId,
            now);

        _transactions.Add(transaction);
        _gatewayTransactions.Add(new PaymentGatewayTransaction(
            transaction.Id,
            PaymentGatewayName.Saman,
            gatewayResult.Token,
            gatewayResult.RequestPayload,
            gatewayResult.ResponsePayload,
            true,
            now));

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        _auditScope.EntityId = transaction.Id.ToString();

        return Result.Success(new OnlinePaymentInitiationDto(
            transaction.Id,
            transaction.TrackingCode,
            gatewayResult.Token,
            gatewayResult.PaymentUrl ?? string.Empty));
    }
}

public sealed class VerifyOnlinePaymentCommandHandler
    : IRequestHandler<VerifyOnlinePaymentCommand, Result<TransactionDto>>
{
    private readonly ITransactionRepository _transactions;
    private readonly IPaymentGatewayTransactionRepository _gatewayTransactions;
    private readonly ICustomerRepository _customers;
    private readonly ISamanGateway _gateway;
    private readonly PaymentSettlementService _settlement;
    private readonly IPaymentUnitOfWork _unitOfWork;
    private readonly FoundationCurrentUser _currentUser;
    private readonly IDateTimeProvider _clock;
    private readonly PaymentDtoMapper _mapper;

    public VerifyOnlinePaymentCommandHandler(
        ITransactionRepository transactions,
        IPaymentGatewayTransactionRepository gatewayTransactions,
        ICustomerRepository customers,
        ISamanGateway gateway,
        PaymentSettlementService settlement,
        IPaymentUnitOfWork unitOfWork,
        FoundationCurrentUser currentUser,
        IDateTimeProvider clock,
        PaymentDtoMapper mapper)
    {
        _transactions = transactions;
        _gatewayTransactions = gatewayTransactions;
        _customers = customers;
        _gateway = gateway;
        _settlement = settlement;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
        _clock = clock;
        _mapper = mapper;
    }

    public async Task<Result<TransactionDto>> Handle(
        VerifyOnlinePaymentCommand request,
        CancellationToken cancellationToken)
    {
        var transaction = await _transactions.GetByTrackingCodeAsync(
            request.TrackingCode,
            cancellationToken);

        if (transaction is null)
        {
            return Result.Failure<TransactionDto>("تراکنش یافت نشد.", "PAYMENTS_TRANSACTION_NOT_FOUND");
        }

        if (transaction.Status != TransactionStatus.Pending)
        {
            return Result.Failure<TransactionDto>(
                "تراکنش در وضعیت قابل بررسی نیست.",
                "PAYMENTS_TRANSACTION_STATUS_INVALID");
        }

        var token = string.IsNullOrWhiteSpace(request.Token)
            ? transaction.ReferenceNumber
            : request.Token;

        if (string.IsNullOrWhiteSpace(token))
        {
            return Result.Failure<TransactionDto>("توکن پرداخت موجود نیست.", "PAYMENTS_TOKEN_MISSING");
        }

        var gatewayResult = await _gateway.VerifyPaymentAsync(
            token,
            transaction.Amount,
            cancellationToken);

        _gatewayTransactions.Add(new PaymentGatewayTransaction(
            transaction.Id,
            PaymentGatewayName.Saman,
            token,
            $"verify token={token} amount={transaction.Amount}",
            gatewayResult.ResponsePayload,
            gatewayResult.Success,
            _clock.UtcNow));

        if (!gatewayResult.Success)
        {
            transaction.MarkFailed(gatewayResult.ResponsePayload, _currentUser.UserId, _clock.UtcNow);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            return Result.Failure<TransactionDto>(
                gatewayResult.Error ?? "تأیید پرداخت ناموفق بود.",
                "PAYMENTS_GATEWAY_VERIFY_FAILED");
        }

        var settlement = await _settlement.SettleAsync(
            transaction,
            gatewayResult.ReferenceNumber,
            gatewayResult.ResponsePayload,
            cancellationToken);

        if (settlement.IsFailure)
        {
            return Result.Failure<TransactionDto>(
                settlement.Errors,
                settlement.Message ?? "ثبت پرداخت ناموفق بود.");
        }

        var customer = await _customers.GetByIdAsync(transaction.CustomerId, false, cancellationToken);
        var invoiceNumber = settlement.Value?.Invoice?.InvoiceNumber;

        return Result.Success(_mapper.ToDto(transaction, invoiceNumber, customer?.DisplayName));
    }
}

public sealed class GetTransactionsQueryHandler
    : IRequestHandler<GetTransactionsQuery, Result<PagedResult<TransactionListItemDto>>>
{
    private readonly ITransactionReadRepository _readRepository;

    public GetTransactionsQueryHandler(ITransactionReadRepository readRepository)
    {
        _readRepository = readRepository;
    }

    public async Task<Result<PagedResult<TransactionListItemDto>>> Handle(
        GetTransactionsQuery request,
        CancellationToken cancellationToken)
    {
        var page = await _readRepository.SearchAsync(
            new TransactionSearchFilter(
                request.Status,
                request.CustomerId,
                request.InvoiceId,
                request.FromUtc,
                request.ToUtc,
                request.Page,
                request.PageSize),
            cancellationToken);

        return Result.Success(page);
    }
}

public sealed class GetCustomerTransactionsQueryHandler
    : IRequestHandler<GetCustomerTransactionsQuery, Result<IReadOnlyList<TransactionListItemDto>>>
{
    private readonly ICustomerRepository _customers;
    private readonly ITransactionReadRepository _readRepository;
    private readonly FoundationCurrentUser _currentUser;

    public GetCustomerTransactionsQueryHandler(
        ICustomerRepository customers,
        ITransactionReadRepository readRepository,
        FoundationCurrentUser currentUser)
    {
        _customers = customers;
        _readRepository = readRepository;
        _currentUser = currentUser;
    }

    public async Task<Result<IReadOnlyList<TransactionListItemDto>>> Handle(
        GetCustomerTransactionsQuery request,
        CancellationToken cancellationToken)
    {
        if (_currentUser.UserId is null)
        {
            return Result.Failure<IReadOnlyList<TransactionListItemDto>>(
                "کاربر جاری قابل تشخیص نیست.",
                "PAYMENTS_CURRENT_USER_MISSING");
        }

        var customer = await _customers.GetByUserIdAsync(
            _currentUser.UserId.Value,
            false,
            cancellationToken);

        if (customer is null)
        {
            return Result.Failure<IReadOnlyList<TransactionListItemDto>>(
                "پروفایل مشتری یافت نشد.",
                "PAYMENTS_CUSTOMER_NOT_FOUND");
        }

        var transactions = await _readRepository.GetCustomerTransactionsAsync(customer.Id, cancellationToken);
        return Result.Success(transactions);
    }
}
