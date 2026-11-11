using System.Text.Json;
using CrmSuite.BuildingBlocks.Application.Abstractions;
using CrmSuite.BuildingBlocks.Domain.Results;
using CrmSuite.Modules.Billing.Application.Abstractions;
using CrmSuite.Modules.Billing.Domain.Entities;
using CrmSuite.Modules.Billing.Domain.Enums;
using CrmSuite.Modules.Foundation.Application.Abstractions;
using CrmSuite.Modules.Orders.Application.Abstractions;
using CrmSuite.Modules.Orders.Domain.Entities;
using CrmSuite.Modules.Workflow.Application.Sales;
using CrmSuite.Modules.Workflow.Domain.Enums;
using MediatR;
using FoundationCurrentUser = CrmSuite.Modules.Foundation.Application.Abstractions.ICurrentUserService;

namespace CrmSuite.Modules.Workflow.Application.Actions;

public sealed class IssueProformaAction : IWorkflowAction
{
    private readonly WorkflowBillingActionSupport _billing;

    public IssueProformaAction(WorkflowBillingActionSupport billing)
    {
        _billing = billing;
    }

    public WorkflowActionKey Key => WorkflowActionKey.IssueProforma;

    public Task<Result> ExecuteAsync(
        WorkflowActionContext context,
        CancellationToken cancellationToken) =>
        _billing.CreateInvoiceAsync(
            context.Order,
            BillingInvoiceType.Proforma,
            context.ActionDataJson,
            issue: false,
            cancellationToken);
}

public sealed class IssueFinalInvoiceAction : IWorkflowAction
{
    private readonly WorkflowBillingActionSupport _billing;

    public IssueFinalInvoiceAction(WorkflowBillingActionSupport billing)
    {
        _billing = billing;
    }

    public WorkflowActionKey Key => WorkflowActionKey.IssueFinalInvoice;

    public Task<Result> ExecuteAsync(
        WorkflowActionContext context,
        CancellationToken cancellationToken) =>
        _billing.CreateInvoiceAsync(
            context.Order,
            WorkflowBillingActionSupport.ParseInvoiceType(context.ActionDataJson),
            context.ActionDataJson,
            issue: true,
            cancellationToken);
}

public sealed class RequestPaymentAction : IWorkflowAction
{
    private readonly WorkflowBillingActionSupport _billing;

    public RequestPaymentAction(WorkflowBillingActionSupport billing)
    {
        _billing = billing;
    }

    public WorkflowActionKey Key => WorkflowActionKey.RequestPayment;

    public async Task<Result> ExecuteAsync(
        WorkflowActionContext context,
        CancellationToken cancellationToken)
    {
        if (!await _billing.IsPaymentSatisfiedAsync(
                context.State.Code,
                context.ActionDataJson,
                context.Order.Id,
                cancellationToken))
        {
            return Result.Failure(
                "پرداخت فاکتور تکمیل نشده است؛ ابتدا پرداخت یا تأیید مشتری را انجام دهید.",
                "WORKFLOW_PAYMENT_REQUIRED");
        }

        return Result.Success();
    }
}

public sealed class ActivateServiceAction : IWorkflowAction
{
    private readonly IMediator _mediator;

    public ActivateServiceAction(IMediator mediator)
    {
        _mediator = mediator;
    }

    public WorkflowActionKey Key => WorkflowActionKey.ActivateService;

    public async Task<Result> ExecuteAsync(
        WorkflowActionContext context,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            new ActivateServiceCommand(context.Order.Id),
            cancellationToken);
        return result.IsFailure
            ? Result.Failure(result.Errors, result.Message)
            : Result.Success();
    }
}

/// <summary>All invoice/payment logic of workflow actions lives here.</summary>
public sealed class WorkflowBillingActionSupport
{
    private readonly IInvoiceRepository _invoices;
    private readonly IBillingUnitOfWork _billingUnitOfWork;
    private readonly ISequenceService _sequences;
    private readonly IDateTimeProvider _clock;
    private readonly FoundationCurrentUser _currentUser;

    public WorkflowBillingActionSupport(
        IInvoiceRepository invoices,
        IBillingUnitOfWork billingUnitOfWork,
        ISequenceService sequences,
        IDateTimeProvider clock,
        FoundationCurrentUser currentUser)
    {
        _invoices = invoices;
        _billingUnitOfWork = billingUnitOfWork;
        _sequences = sequences;
        _clock = clock;
        _currentUser = currentUser;
    }

    public async Task<Result> CreateInvoiceAsync(
        Order order,
        BillingInvoiceType type,
        string actionData,
        bool issue,
        CancellationToken cancellationToken)
    {
        var item = order.Items.FirstOrDefault(item => item.ServiceId.HasValue);
        if (item is null)
        {
            return Result.Failure(
                "آیتم سرویس سفارش یافت نشد.",
                "WORKFLOW_ORDER_SERVICE_ITEM_MISSING");
        }

        using var document = JsonDocument.Parse(
            string.IsNullOrWhiteSpace(actionData) ? "{}" : actionData);
        var root = document.RootElement;
        var amount = ReadDecimal(root, "amount") ?? item.EffectivePrice;
        var paymentMode = ReadString(root, "paymentMode") ?? "Gateway";
        var requiresPayment = ReadBoolean(root, "requiresPayment") ?? true;

        var now = _clock.UtcNow;
        var number = await _sequences.GetNextValueAsync("InvoiceNumber", cancellationToken);
        var invoice = new Invoice(
            number,
            order.Id,
            order.CustomerId,
            type,
            now,
            now.AddDays(7),
            0,
            _currentUser.UserId,
            now);
        invoice.SetPaymentTerms(paymentMode, requiresPayment);
        invoice.AddItem(new InvoiceItem(invoice.Id, item.ItemName, 1, amount, now));

        if (issue)
        {
            invoice.Issue(0, 0, _currentUser.UserId, now);
        }
        else
        {
            invoice.RecalculateDraftTotal();
        }

        _invoices.Add(invoice);
        await _billingUnitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }

    public async Task<bool> IsPaymentSatisfiedAsync(
        string stateCode,
        string actionData,
        Guid orderId,
        CancellationToken cancellationToken)
    {
        using var document = JsonDocument.Parse(
            string.IsNullOrWhiteSpace(actionData) ? "{}" : actionData);
        var root = document.RootElement;

        var invoices = await _invoices.GetByOrderAsync(orderId, cancellationToken);

        if (string.Equals(stateCode, "CUSTOMER_APPROVAL", StringComparison.OrdinalIgnoreCase))
        {
            var payable = invoices
                .Where(invoice => invoice.RequiresPayment)
                .OrderByDescending(invoice => invoice.CreatedAt)
                .FirstOrDefault();
            return payable is null || payable.Status == InvoiceStatus.Paid;
        }

        if (ReadBoolean(root, "paid") ?? false)
        {
            return invoices.Any(invoice => invoice.Status == InvoiceStatus.Paid);
        }

        return false;
    }

    public static BillingInvoiceType ParseInvoiceType(string actionData)
    {
        using var document = JsonDocument.Parse(
            string.IsNullOrWhiteSpace(actionData) ? "{}" : actionData);
        var value = ReadString(document.RootElement, "invoiceType");
        return Enum.TryParse<BillingInvoiceType>(value, true, out var parsed)
            ? parsed
            : BillingInvoiceType.Official;
    }

    private static string? ReadString(JsonElement root, string name) =>
        root.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String
            ? value.GetString()
            : null;

    private static decimal? ReadDecimal(JsonElement root, string name) =>
        root.TryGetProperty(name, out var value) && value.TryGetDecimal(out var parsed)
            ? parsed
            : null;

    private static bool? ReadBoolean(JsonElement root, string name) =>
        root.TryGetProperty(name, out var value) && value.ValueKind is JsonValueKind.True or JsonValueKind.False
            ? value.GetBoolean()
            : null;
}
