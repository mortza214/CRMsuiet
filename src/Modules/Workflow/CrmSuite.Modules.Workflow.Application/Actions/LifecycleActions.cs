using System.Text.Json;
using CrmSuite.BuildingBlocks.Application.Abstractions;
using CrmSuite.BuildingBlocks.Domain.Results;
using CrmSuite.Modules.Billing.Application.Abstractions;
using CrmSuite.Modules.Billing.Domain.Enums;
using CrmSuite.Modules.Orders.Application.Abstractions;
using CrmSuite.Modules.Workflow.Domain.Enums;
using FoundationCurrentUser = CrmSuite.Modules.Foundation.Application.Abstractions.ICurrentUserService;

namespace CrmSuite.Modules.Workflow.Application.Actions;

public sealed class SelectEquipmentAction : IWorkflowAction
{
    private readonly IOrderUnitOfWork _unitOfWork;
    private readonly IDateTimeProvider _clock;
    private readonly FoundationCurrentUser _currentUser;

    public SelectEquipmentAction(
        IOrderUnitOfWork unitOfWork,
        IDateTimeProvider clock,
        FoundationCurrentUser currentUser)
    {
        _unitOfWork = unitOfWork;
        _clock = clock;
        _currentUser = currentUser;
    }

    public WorkflowActionKey Key => WorkflowActionKey.SelectEquipment;

    public async Task<Result> ExecuteAsync(
        WorkflowActionContext context,
        CancellationToken cancellationToken)
    {
        var mode = ReadString(context.ActionDataJson, "mode");
        if (mode is not ("Purchase" or "Consigned" or "Existing"))
        {
            return Result.Failure(
                "حالت تأمین تجهیزات را انتخاب کنید.",
                "WORKFLOW_EQUIPMENT_MODE_REQUIRED");
        }

        context.Order.SetEquipmentConsigned(
            mode == "Consigned",
            _currentUser.UserId,
            _clock.UtcNow);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }

    private static string? ReadString(string data, string name)
    {
        using var document = JsonDocument.Parse(string.IsNullOrWhiteSpace(data) ? "{}" : data);
        return document.RootElement.TryGetProperty(name, out var value)
               && value.ValueKind == JsonValueKind.String
            ? value.GetString()
            : null;
    }
}

public sealed class PurchaseEquipmentAction : IWorkflowAction
{
    public WorkflowActionKey Key => WorkflowActionKey.PurchaseEquipment;

    public Task<Result> ExecuteAsync(
        WorkflowActionContext context,
        CancellationToken cancellationToken) =>
        Task.FromResult(Result.Success());
}

public sealed class CustomerAction : IWorkflowAction
{
    public WorkflowActionKey Key => WorkflowActionKey.CustomerAction;

    public Task<Result> ExecuteAsync(
        WorkflowActionContext context,
        CancellationToken cancellationToken)
    {
        using var document = JsonDocument.Parse(
            string.IsNullOrWhiteSpace(context.ActionDataJson) ? "{}" : context.ActionDataJson);
        var decision = document.RootElement.TryGetProperty("decision", out var value)
                       && value.ValueKind == JsonValueKind.String
            ? value.GetString()
            : null;
        return decision is not ("approve" or "cancel")
            ? Task.FromResult(Result.Failure(
                "پاسخ مشتری (تأیید یا انصراف) الزامی است.",
                "WORKFLOW_CUSTOMER_DECISION_REQUIRED"))
            : Task.FromResult(Result.Success());
    }
}

public sealed class PayInvoiceAction : IWorkflowAction
{
    private readonly IInvoiceRepository _invoices;

    public PayInvoiceAction(IInvoiceRepository invoices)
    {
        _invoices = invoices;
    }

    public WorkflowActionKey Key => WorkflowActionKey.PayInvoice;

    public async Task<Result> ExecuteAsync(
        WorkflowActionContext context,
        CancellationToken cancellationToken)
    {
        using var document = JsonDocument.Parse(
            string.IsNullOrWhiteSpace(context.ActionDataJson) ? "{}" : context.ActionDataJson);
        var paid = document.RootElement.TryGetProperty("paid", out var value)
                   && value.ValueKind == JsonValueKind.True;
        if (paid)
        {
            var invoices = await _invoices.GetByOrderAsync(context.Order.Id, cancellationToken);
            if (invoices.Any(invoice => invoice.Status == InvoiceStatus.Paid))
            {
                return Result.Success();
            }
        }

        return Result.Failure(
            "پرداخت فاکتور تأیید نشده است.",
            "WORKFLOW_PAYMENT_NOT_CONFIRMED");
    }
}

public sealed class VerifyPaymentAction : IWorkflowAction
{
    public WorkflowActionKey Key => WorkflowActionKey.VerifyPayment;

    public Task<Result> ExecuteAsync(
        WorkflowActionContext context,
        CancellationToken cancellationToken)
    {
        using var document = JsonDocument.Parse(
            string.IsNullOrWhiteSpace(context.ActionDataJson) ? "{}" : context.ActionDataJson);
        var approved = document.RootElement.TryGetProperty("approved", out var value)
                       && value.ValueKind == JsonValueKind.True;
        return approved
            ? Task.FromResult(Result.Success())
            : Task.FromResult(Result.Failure(
                "تأیید مالی پرداخت الزامی است.",
                "WORKFLOW_FINANCE_PAYMENT_NOT_APPROVED"));
    }
}

public sealed class SignContractAction : IWorkflowAction
{
    public WorkflowActionKey Key => WorkflowActionKey.SignContract;

    public Task<Result> ExecuteAsync(
        WorkflowActionContext context,
        CancellationToken cancellationToken) =>
        Task.FromResult(Result.Success());
}
