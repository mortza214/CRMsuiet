using CrmSuite.BuildingBlocks.Application.Abstractions;
using CrmSuite.BuildingBlocks.Domain.Exceptions;
using CrmSuite.BuildingBlocks.Domain.Results;
using CrmSuite.Modules.Billing.Application.Abstractions;
using CrmSuite.Modules.Billing.Application.Notifications;
using CrmSuite.Modules.Billing.Domain.Entities;
using CrmSuite.Modules.Billing.Domain.Enums;
using CrmSuite.Modules.Foundation.Application.Abstractions;
using CrmSuite.Modules.Orders.Application.Abstractions;
using CrmSuite.Modules.Orders.Domain.Entities;
using CrmSuite.Modules.Orders.Domain.Enums;
using CrmSuite.Modules.Payments.Application.Abstractions;
using CrmSuite.Modules.Payments.Domain.Entities;
using CrmSuite.Modules.Workflow.Application.Sales;
using MediatR;
using FoundationCurrentUser = CrmSuite.Modules.Foundation.Application.Abstractions.ICurrentUserService;

namespace CrmSuite.Modules.Payments.Application.Services;

public sealed record SettlementOutcome(Invoice? Invoice, Order? Order);

public sealed class PaymentSettlementService
{
    private readonly IInvoiceRepository _invoices;
    private readonly IOrderRepository _orders;
    private readonly IPaymentUnitOfWork _unitOfWork;
    private readonly IMediator _mediator;
    private readonly IAuditScope _auditScope;
    private readonly FoundationCurrentUser _currentUser;
    private readonly IDateTimeProvider _clock;

    public PaymentSettlementService(
        IInvoiceRepository invoices,
        IOrderRepository orders,
        IPaymentUnitOfWork unitOfWork,
        IMediator mediator,
        IAuditScope auditScope,
        FoundationCurrentUser currentUser,
        IDateTimeProvider clock)
    {
        _invoices = invoices;
        _orders = orders;
        _unitOfWork = unitOfWork;
        _mediator = mediator;
        _auditScope = auditScope;
        _currentUser = currentUser;
        _clock = clock;
    }

    public async Task<Result<SettlementOutcome>> SettleAsync(
        Transaction transaction,
        string? referenceNumber,
        string? gatewayResponse,
        CancellationToken cancellationToken)
    {
        if (transaction.InvoiceId is null)
        {
            try
            {
                transaction.MarkSuccess(referenceNumber, gatewayResponse, _currentUser.UserId, _clock.UtcNow);
            }
            catch (DomainException exception)
            {
                return Result.Failure<SettlementOutcome>(exception.Message, exception.Code);
            }

            await _unitOfWork.SaveChangesAsync(cancellationToken);
            _auditScope.EntityId = transaction.Id.ToString();
            return Result.Success(new SettlementOutcome(null, null));
        }

        var invoice = await _invoices.GetByIdWithItemsAsync(transaction.InvoiceId.Value, cancellationToken);
        if (invoice is null)
        {
            return Result.Failure<SettlementOutcome>("فاکتور یافت نشد.", "PAYMENTS_INVOICE_NOT_FOUND");
        }

        if (invoice.CustomerId != transaction.CustomerId)
        {
            return Result.Failure<SettlementOutcome>(
                "فاکتور به مشتری تراکنش تعلق ندارد.",
                "PAYMENTS_INVOICE_CUSTOMER_MISMATCH");
        }

        var order = await _orders.GetByIdWithDetailsAsync(invoice.OrderId, cancellationToken);
        if (order is null)
        {
            return Result.Failure<SettlementOutcome>("سفارش فاکتور یافت نشد.", "PAYMENTS_ORDER_NOT_FOUND");
        }

        if (order.Status is OrderStatus.Draft or OrderStatus.Canceled)
        {
            return Result.Failure<SettlementOutcome>(
                "سفارش در وضعیت قابل پرداخت نیست.",
                "PAYMENTS_ORDER_STATUS_INVALID");
        }

        try
        {
            invoice.RegisterPayment(transaction.Amount, _currentUser.UserId, _clock.UtcNow);

            if (invoice.Status == InvoiceStatus.Paid && order.Status == OrderStatus.PendingPayment)
            {
                order.ChangeStatus(
                    OrderStatus.Paid,
                    _currentUser.UserId,
                    _clock.UtcNow,
                    $"پرداخت {transaction.TrackingCode} تأیید شد.");
            }

            transaction.MarkSuccess(referenceNumber, gatewayResponse, _currentUser.UserId, _clock.UtcNow);
        }
        catch (DomainException exception)
        {
            return Result.Failure<SettlementOutcome>(exception.Message, exception.Code);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        _auditScope.EntityId = transaction.Id.ToString();

        if (invoice.Status == InvoiceStatus.Paid)
        {
            try
            {
                await _mediator.Publish(
                    new InvoicePaidNotification(
                        invoice.Id,
                        invoice.InvoiceNumber,
                        invoice.CustomerId,
                        invoice.TotalAmount),
                    cancellationToken);
            }
            catch
            {
                // Notification delivery must not roll back a settled invoice.
            }

            if (invoice.Type == BillingInvoiceType.Proforma)
            {
                try
                {
                    await _mediator.Send(
                        new ActivateServiceCommand(order.Id),
                        cancellationToken);
                }
                catch
                {
                    // Payment is final even if activation needs a manual follow-up.
                }
            }
        }

        return Result.Success(new SettlementOutcome(invoice, order));
    }
}
