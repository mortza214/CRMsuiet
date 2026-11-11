using System.Globalization;
using CrmSuite.BuildingBlocks.Application.Abstractions;
using CrmSuite.BuildingBlocks.Domain.Results;
using CrmSuite.Modules.Billing.Application.Abstractions;
using CrmSuite.Modules.Billing.Application.Dtos;
using CrmSuite.Modules.Billing.Application.Mapping;
using CrmSuite.Modules.Billing.Domain.Entities;
using CrmSuite.Modules.Billing.Domain.Enums;
using CrmSuite.Modules.Customers.Application.Abstractions;
using CrmSuite.Modules.Foundation.Application.Abstractions;
using CrmSuite.Modules.Orders.Application.Queries;
using MediatR;
using FoundationCurrentUser = CrmSuite.Modules.Foundation.Application.Abstractions.ICurrentUserService;

namespace CrmSuite.Modules.Billing.Application.Commands;

public sealed class GenerateInvoiceFromOrderCommandHandler
    : IRequestHandler<GenerateInvoiceFromOrderCommand, Result<InvoiceDto>>
{
    private readonly IInvoiceRepository _invoices;
    private readonly IBillingUnitOfWork _unitOfWork;
    private readonly IMediator _mediator;
    private readonly ICustomerRepository _customers;
    private readonly ISettingService _settings;
    private readonly ISequenceService _sequences;
    private readonly IAuditScope _auditScope;
    private readonly FoundationCurrentUser _currentUser;
    private readonly IDateTimeProvider _clock;
    private readonly BillingDtoMapper _mapper;

    public GenerateInvoiceFromOrderCommandHandler(
        IInvoiceRepository invoices,
        IBillingUnitOfWork unitOfWork,
        IMediator mediator,
        ICustomerRepository customers,
        ISettingService settings,
        ISequenceService sequences,
        IAuditScope auditScope,
        FoundationCurrentUser currentUser,
        IDateTimeProvider clock,
        BillingDtoMapper mapper)
    {
        _invoices = invoices;
        _unitOfWork = unitOfWork;
        _mediator = mediator;
        _customers = customers;
        _settings = settings;
        _sequences = sequences;
        _auditScope = auditScope;
        _currentUser = currentUser;
        _clock = clock;
        _mapper = mapper;
    }

    public async Task<Result<InvoiceDto>> Handle(
        GenerateInvoiceFromOrderCommand request,
        CancellationToken cancellationToken)
    {
        var orderResult = await _mediator.Send(new GetOrderByIdQuery(request.OrderId), cancellationToken);
        if (orderResult.IsFailure || orderResult.Value is null)
        {
            return Result.Failure<InvoiceDto>(
                orderResult.Errors,
                orderResult.Message ?? "سفارش یافت نشد.");
        }

        var order = orderResult.Value;
        if (order.Status is not ("PendingPayment" or "Paid"))
        {
            return Result.Failure<InvoiceDto>(
                "فاکتور فقط برای سفارش‌های در انتظار پرداخت یا پرداخت‌شده صادر می‌شود.",
                "BILLING_ORDER_STATUS_INVALID");
        }

        if (order.Items.Count == 0)
        {
            return Result.Failure<InvoiceDto>("سفارش آیتمی ندارد.", "BILLING_ORDER_EMPTY");
        }

        if (await _invoices.ExistsForOrderAsync(request.OrderId, request.Type, cancellationToken))
        {
            return Result.Failure<InvoiceDto>(
                "برای این سفارش قبلاً فاکتور با همین نوع صادر شده است.",
                "BILLING_INVOICE_DUPLICATE");
        }

        var now = _clock.UtcNow;
        var vat = await GetPercentageAsync("Billing.VatPercentage", cancellationToken);
        var insurance = await GetPercentageAsync("Billing.InsurancePercentage", cancellationToken);
        var invoiceNumber = await _sequences.GetNextValueAsync("InvoiceNumber", cancellationToken);
        var dueDate = request.DueDateUtc ?? now.AddDays(7);

        var invoice = new Invoice(
            invoiceNumber,
            order.Id,
            order.CustomerId,
            request.Type,
            now,
            dueDate,
            order.DiscountAmount,
            _currentUser.UserId,
            now);

        foreach (var item in order.Items)
        {
            invoice.AddItem(new InvoiceItem(
                invoice.Id,
                item.ItemName,
                item.Quantity,
                item.UnitPrice,
                now));
        }

        invoice.Issue(vat, insurance, _currentUser.UserId, now);
        _invoices.Add(invoice);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        _auditScope.EntityId = invoice.Id.ToString();

        return Result.Success(_mapper.ToInvoiceDto(invoice, order.CustomerName, order.OrderNumber));
    }

    private async Task<decimal> GetPercentageAsync(string key, CancellationToken cancellationToken)
    {
        var setting = await _settings.GetAsync(key, includeSecret: false, cancellationToken);

        return decimal.TryParse(
            setting?.Value,
            NumberStyles.Number,
            CultureInfo.InvariantCulture,
            out var value)
            ? value
            : 0m;
    }
}

public sealed class GenerateContractCommandHandler
    : IRequestHandler<GenerateContractCommand, Result<ContractDto>>
{
    private readonly IContractRepository _contracts;
    private readonly IBillingUnitOfWork _unitOfWork;
    private readonly IMediator _mediator;
    private readonly ICustomerRepository _customers;
    private readonly ISettingService _settings;
    private readonly ISequenceService _sequences;
    private readonly IAuditScope _auditScope;
    private readonly FoundationCurrentUser _currentUser;
    private readonly IDateTimeProvider _clock;
    private readonly BillingDtoMapper _mapper;

    public GenerateContractCommandHandler(
        IContractRepository contracts,
        IBillingUnitOfWork unitOfWork,
        IMediator mediator,
        ICustomerRepository customers,
        ISettingService settings,
        ISequenceService sequences,
        IAuditScope auditScope,
        FoundationCurrentUser currentUser,
        IDateTimeProvider clock,
        BillingDtoMapper mapper)
    {
        _contracts = contracts;
        _unitOfWork = unitOfWork;
        _mediator = mediator;
        _customers = customers;
        _settings = settings;
        _sequences = sequences;
        _auditScope = auditScope;
        _currentUser = currentUser;
        _clock = clock;
        _mapper = mapper;
    }

    public async Task<Result<ContractDto>> Handle(
        GenerateContractCommand request,
        CancellationToken cancellationToken)
    {
        var customer = await _customers.GetByIdAsync(request.CustomerId, false, cancellationToken);
        if (customer is null)
        {
            return Result.Failure<ContractDto>("مشتری یافت نشد.", "BILLING_CUSTOMER_NOT_FOUND");
        }

        string? orderNumber = null;
        if (request.OrderId.HasValue)
        {
            var orderResult = await _mediator.Send(new GetOrderByIdQuery(request.OrderId.Value), cancellationToken);
            if (orderResult.IsFailure || orderResult.Value is null)
            {
                return Result.Failure<ContractDto>("سفارش یافت نشد.", "BILLING_ORDER_NOT_FOUND");
            }

            if (orderResult.Value.CustomerId != request.CustomerId)
            {
                return Result.Failure<ContractDto>(
                    "سفارش به مشتری انتخاب‌شده تعلق ندارد.",
                    "BILLING_ORDER_CUSTOMER_MISMATCH");
            }

            orderNumber = orderResult.Value.OrderNumber;
        }

        var now = _clock.UtcNow;
        var contractNumber = await _sequences.GetNextValueAsync("ContractNumber", cancellationToken);
        var templateSetting = await _settings.GetAsync("Billing.ContractTemplate", false, cancellationToken);

        var template = string.IsNullOrWhiteSpace(templateSetting?.Value)
            ? "قرارداد {ContractNumber}\nمشتری: {CustomerName}\nاز {StartDate} تا {EndDate}\nسفارش: {OrderNumber}"
            : templateSetting.Value;

        var content = request.Content ?? template
            .Replace("{ContractNumber}", contractNumber, StringComparison.Ordinal)
            .Replace("{CustomerName}", customer.DisplayName, StringComparison.Ordinal)
            .Replace("{StartDate}", request.StartDateUtc.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture), StringComparison.Ordinal)
            .Replace("{EndDate}", request.EndDateUtc.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture), StringComparison.Ordinal)
            .Replace("{OrderNumber}", orderNumber ?? "-", StringComparison.Ordinal);

        var contract = new Contract(
            contractNumber,
            request.CustomerId,
            request.OrderId,
            request.StartDateUtc,
            request.EndDateUtc,
            content,
            _currentUser.UserId,
            now);

        _contracts.Add(contract);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        _auditScope.EntityId = contract.Id.ToString();

        return Result.Success(_mapper.ToContractDto(contract, customer.DisplayName));
    }
}

public sealed class UpdateInvoiceStatusCommandHandler
    : IRequestHandler<UpdateInvoiceStatusCommand, Result<InvoiceDto>>
{
    private readonly IInvoiceRepository _invoices;
    private readonly IBillingUnitOfWork _unitOfWork;
    private readonly ICustomerRepository _customers;
    private readonly IAuditScope _auditScope;
    private readonly FoundationCurrentUser _currentUser;
    private readonly IDateTimeProvider _clock;
    private readonly BillingDtoMapper _mapper;

    public UpdateInvoiceStatusCommandHandler(
        IInvoiceRepository invoices,
        IBillingUnitOfWork unitOfWork,
        ICustomerRepository customers,
        IAuditScope auditScope,
        FoundationCurrentUser currentUser,
        IDateTimeProvider clock,
        BillingDtoMapper mapper)
    {
        _invoices = invoices;
        _unitOfWork = unitOfWork;
        _customers = customers;
        _auditScope = auditScope;
        _currentUser = currentUser;
        _clock = clock;
        _mapper = mapper;
    }

    public async Task<Result<InvoiceDto>> Handle(
        UpdateInvoiceStatusCommand request,
        CancellationToken cancellationToken)
    {
        var invoice = await _invoices.GetByIdWithItemsAsync(request.InvoiceId, cancellationToken);
        if (invoice is null)
        {
            return Result.Failure<InvoiceDto>("فاکتور یافت نشد.", "BILLING_INVOICE_NOT_FOUND");
        }

        var customer = await _customers.GetByIdAsync(invoice.CustomerId, false, cancellationToken);
        _auditScope.OldValues = _mapper.ToInvoiceDto(invoice, customer?.DisplayName);
        _auditScope.EntityId = invoice.Id.ToString();

        try
        {
            invoice.UpdateStatus(request.Status, _currentUser.UserId, _clock.UtcNow);
        }
        catch (CrmSuite.BuildingBlocks.Domain.Exceptions.DomainException exception)
        {
            return Result.Failure<InvoiceDto>(exception.Message, exception.Code);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        var updated = await _invoices.GetByIdWithItemsAsync(invoice.Id, cancellationToken);

        return Result.Success(_mapper.ToInvoiceDto(updated ?? invoice, customer?.DisplayName));
    }
}
