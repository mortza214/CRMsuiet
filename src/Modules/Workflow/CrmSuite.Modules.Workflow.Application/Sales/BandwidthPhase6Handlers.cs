using CrmSuite.BuildingBlocks.Application.Abstractions;
using CrmSuite.BuildingBlocks.Domain.Results;
using CrmSuite.Modules.Billing.Application.Abstractions;
using CrmSuite.Modules.Billing.Domain.Entities;
using CrmSuite.Modules.Billing.Domain.Enums;
using CrmSuite.Modules.Catalog.Application.Abstractions;
using CrmSuite.Modules.Customers.Application.Abstractions;
using CrmSuite.Modules.Forms.Application.Abstractions;
using CrmSuite.Modules.Forms.Application.Commands;
using CrmSuite.Modules.Forms.Domain.Enums;
using CrmSuite.Modules.Foundation.Application.Abstractions;
using CrmSuite.Modules.Orders.Application.Abstractions;
using CrmSuite.Modules.Orders.Domain.Entities;
using CrmSuite.Modules.Orders.Domain.Enums;
using CrmSuite.Modules.Workflow.Application.Commands;
using CrmSuite.Modules.Workflow.Application.Dtos;
using CrmSuite.Modules.Workflow.Application.Queries;
using CrmSuite.Modules.Workflow.Domain.Entities;
using CrmSuite.Modules.Workflow.Domain.Enums;
using MediatR;
using FoundationCurrentUser = CrmSuite.Modules.Foundation.Application.Abstractions.ICurrentUserService;

namespace CrmSuite.Modules.Workflow.Application.Sales;

public sealed class BandwidthPhase6Support
{
    private readonly BandwidthWorkflowSupport _workflow;
    private readonly IOrderRepository _orders;
    private readonly IFormTemplateRepository _forms;
    private readonly IMediator _mediator;

    public BandwidthPhase6Support(
        BandwidthWorkflowSupport workflow,
        IOrderRepository orders,
        IFormTemplateRepository forms,
        IMediator mediator)
    {
        _workflow = workflow;
        _orders = orders;
        _forms = forms;
        _mediator = mediator;
    }

    public async Task<(Order Order, WorkflowInstance Instance, WorkflowDefinition Definition)?> LoadAsync(
        Guid instanceId,
        CancellationToken cancellationToken)
    {
        var instance = await _workflow.GetInstanceAsync(instanceId, cancellationToken);
        if (instance is null)
        {
            return null;
        }

        var active = await _workflow.GetActiveAsync(instance.EntityId, cancellationToken);
        if (active is null)
        {
            return null;
        }

        var order = await _orders.GetByIdWithDetailsAsync(instance.EntityId, cancellationToken);
        return order is null ? null : (order, active.Value.Instance, active.Value.Definition);
    }

    public async Task<WorkflowInstance?> LoadAsyncByOrderAsync(
        Guid orderId,
        CancellationToken cancellationToken)
    {
        var active = await _workflow.GetActiveAsync(orderId, cancellationToken);
        return active?.Instance;
    }

    public async Task<Result> SubmitFormAsync(
        WorkflowState state,
        Order order,
        string dataJson,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(state.FormCode))
        {
            return Result.Success();
        }

        var template = await _forms.GetByCodeAsync(state.FormCode, cancellationToken);
        if (template is null)
        {
            return Result.Failure(
                $"فرم «{state.FormCode}» در ماژول Forms یافت نشد.",
                "SALES_FORM_NOT_FOUND");
        }

        var result = await _mediator.Send(
            new SubmitFormCommand(
                template.Id,
                order.CustomerId,
                order.Id,
                dataJson,
                EntityType: FormEntityType.Order,
                EntityId: order.Id),
            cancellationToken);

        return result.IsFailure
            ? Result.Failure(result.Errors, result.Message)
            : Result.Success();
    }

    public Task<Result<WorkflowInstanceDto>> MoveAsync(
        WorkflowInstance instance,
        WorkflowDefinition definition,
        string target,
        string? payload,
        string? comment,
        CancellationToken cancellationToken) =>
        _workflow.MoveAsync(instance, definition, target, payload, comment, cancellationToken);
}

public sealed class SubmitFeasibilityResultCommandHandler
    : IRequestHandler<SubmitFeasibilityResultCommand, Result<WorkflowInstanceDto>>
{
    private readonly BandwidthPhase6Support _support;
    private readonly FoundationCurrentUser _currentUser;

    public SubmitFeasibilityResultCommandHandler(
        BandwidthPhase6Support support,
        FoundationCurrentUser currentUser)
    {
        _support = support;
        _currentUser = currentUser;
    }

    public async Task<Result<WorkflowInstanceDto>> Handle(
        SubmitFeasibilityResultCommand request,
        CancellationToken cancellationToken)
    {
        var loaded = await _support.LoadAsync(request.InstanceId, cancellationToken);
        if (loaded is null)
        {
            return Result.Failure<WorkflowInstanceDto>("درخواست یا گردش کار یافت نشد.", "SALES_WORKFLOW_NOT_FOUND");
        }

        var (order, instance, definition) = loaded.Value;
        var state = definition.FindState(instance.CurrentStateId);
        var form = await _support.SubmitFormAsync(state, order, request.DataJson, cancellationToken);
        if (form.IsFailure)
        {
            return Result.Failure<WorkflowInstanceDto>(form.Errors, form.Message);
        }

        return await _support.MoveAsync(
            instance,
            definition,
            "FEASIBILITY_REVIEW",
            request.DataJson,
            "ثبت نتیجه امکان‌سنجی",
            cancellationToken);
    }
}

public sealed class ReviewFeasibilityCommandHandler
    : IRequestHandler<ReviewFeasibilityCommand, Result<WorkflowInstanceDto>>
{
    private readonly BandwidthPhase6Support _support;

    public ReviewFeasibilityCommandHandler(BandwidthPhase6Support support)
    {
        _support = support;
    }

    public async Task<Result<WorkflowInstanceDto>> Handle(
        ReviewFeasibilityCommand request,
        CancellationToken cancellationToken)
    {
        var loaded = await _support.LoadAsync(request.InstanceId, cancellationToken);
        if (loaded is null)
        {
            return Result.Failure<WorkflowInstanceDto>("گردش کار یافت نشد.", "SALES_WORKFLOW_NOT_FOUND");
        }

        var (_, instance, definition) = loaded.Value;
        return await _support.MoveAsync(
            instance,
            definition,
            request.Approved ? "EQUIPMENT_SUPPLY" : "FEASIBILITY_PENDING",
            "{\"approved\":" + (request.Approved ? "true" : "false") + "}",
            request.Comment,
            cancellationToken);
    }
}

public sealed class SelectEquipmentSupplyCommandHandler
    : IRequestHandler<SelectEquipmentSupplyCommand, Result<WorkflowInstanceDto>>
{
    private readonly BandwidthPhase6Support _support;

    public SelectEquipmentSupplyCommandHandler(BandwidthPhase6Support support)
    {
        _support = support;
    }

    public async Task<Result<WorkflowInstanceDto>> Handle(
        SelectEquipmentSupplyCommand request,
        CancellationToken cancellationToken)
    {
        var loaded = await _support.LoadAsync(request.InstanceId, cancellationToken);
        if (loaded is null)
        {
            return Result.Failure<WorkflowInstanceDto>("گردش کار یافت نشد.", "SALES_WORKFLOW_NOT_FOUND");
        }

        var (_, instance, definition) = loaded.Value;
        var target = request.Mode == EquipmentSupplyMode.Purchase
            ? "PURCHASE_PENDING"
            : "PROFORMA";
        return await _support.MoveAsync(
            instance,
            definition,
            target,
            "{\"supplyMode\":\"" + request.Mode + "\"}",
            request.Mode.ToString(),
            cancellationToken);
    }
}

public sealed class CompletePurchaseCommandHandler
    : IRequestHandler<CompletePurchaseCommand, Result<WorkflowInstanceDto>>
{
    private readonly BandwidthPhase6Support _support;

    public CompletePurchaseCommandHandler(BandwidthPhase6Support support)
    {
        _support = support;
    }

    public async Task<Result<WorkflowInstanceDto>> Handle(
        CompletePurchaseCommand request,
        CancellationToken cancellationToken)
    {
        var loaded = await _support.LoadAsync(request.InstanceId, cancellationToken);
        if (loaded is null)
        {
            return Result.Failure<WorkflowInstanceDto>("گردش کار یافت نشد.", "SALES_WORKFLOW_NOT_FOUND");
        }

        var (_, instance, definition) = loaded.Value;
        return await _support.MoveAsync(
            instance,
            definition,
            "PROFORMA",
            "{\"purchaseCompleted\":true}",
            "خرید تجهیزات تکمیل شد",
            cancellationToken);
    }
}

public sealed class IssueProformaCommandHandler
    : IRequestHandler<IssueProformaCommand, Result<ServiceDecisionResultDto>>
{
    private readonly BandwidthPhase6Support _support;
    private readonly IOrderRepository _orders;
    private readonly IInvoiceRepository _invoices;
    private readonly IBillingUnitOfWork _billingUnitOfWork;
    private readonly ISequenceService _sequences;
    private readonly FoundationCurrentUser _currentUser;
    private readonly IDateTimeProvider _clock;

    public IssueProformaCommandHandler(
        BandwidthPhase6Support support,
        IOrderRepository orders,
        IInvoiceRepository invoices,
        IBillingUnitOfWork billingUnitOfWork,
        ISequenceService sequences,
        FoundationCurrentUser currentUser,
        IDateTimeProvider clock)
    {
        _support = support;
        _orders = orders;
        _invoices = invoices;
        _billingUnitOfWork = billingUnitOfWork;
        _sequences = sequences;
        _currentUser = currentUser;
        _clock = clock;
    }

    public async Task<Result<ServiceDecisionResultDto>> Handle(
        IssueProformaCommand request,
        CancellationToken cancellationToken)
    {
        var loaded = await _support.LoadAsync(request.InstanceId, cancellationToken);
        if (loaded is null)
        {
            return Result.Failure<ServiceDecisionResultDto>("گردش کار یافت نشد.", "SALES_WORKFLOW_NOT_FOUND");
        }

        var (order, instance, definition) = loaded.Value;
        var item = order.Items.FirstOrDefault(item => item.ServiceId.HasValue);
        if (item is null)
        {
            return Result.Failure<ServiceDecisionResultDto>("آیتم سرویس یافت نشد.", "SALES_SERVICE_ITEM_NOT_FOUND");
        }

        var now = _clock.UtcNow;
        var number = await _sequences.GetNextValueAsync("InvoiceNumber", cancellationToken);
        var invoice = new Invoice(
            number,
            order.Id,
            order.CustomerId,
            request.InvoiceType,
            now,
            now.AddDays(7),
            0,
            _currentUser.UserId,
            now);
        invoice.SetPaymentTerms(request.PaymentMode.ToString(), request.RequiresPayment);
        invoice.AddItem(new InvoiceItem(invoice.Id, item.ItemName, 1, item.EffectivePrice, now));
        invoice.RecalculateDraftTotal();
        _invoices.Add(invoice);
        await _billingUnitOfWork.SaveChangesAsync(cancellationToken);

        var moved = await _support.MoveAsync(
            instance,
            definition,
            "CUSTOMER_APPROVAL",
            "{\"invoiceId\":\"" + invoice.Id + "\",\"paymentMode\":\"" + request.PaymentMode + "\",\"requiresPayment\":" + (request.RequiresPayment ? "true" : "false") + "}",
            "صدور پیش‌فاکتور",
            cancellationToken);

        return moved.IsFailure
            ? Result.Failure<ServiceDecisionResultDto>(moved.Errors, moved.Message)
            : Result.Success(new ServiceDecisionResultDto(moved.Value!, invoice.Id, item.EffectivePrice));
    }
}

public sealed class ConfirmProformaCommandHandler
    : IRequestHandler<ConfirmProformaCommand, Result<WorkflowInstanceDto>>
{
    private readonly BandwidthPhase6Support _support;

    public ConfirmProformaCommandHandler(BandwidthPhase6Support support)
    {
        _support = support;
    }

    public async Task<Result<WorkflowInstanceDto>> Handle(
        ConfirmProformaCommand request,
        CancellationToken cancellationToken)
    {
        var loaded = await _support.LoadAsync(request.InstanceId, cancellationToken);
        if (loaded is null)
        {
            return Result.Failure<WorkflowInstanceDto>("گردش کار یافت نشد.", "SALES_WORKFLOW_NOT_FOUND");
        }

        var (_, instance, definition) = loaded.Value;
        if (!request.Approved)
        {
            return await _support.MoveAsync(
                instance,
                definition,
                "PROFORMA",
                "{\"approved\":false}",
                "مشتری پیش‌فاکتور را نپذیرفت",
                cancellationToken);
        }

        return await _support.MoveAsync(
            instance,
            definition,
            request.NeedsInstallation ? "INSTALLATION_PENDING" : "PAYMENT_PENDING",
            "{\"approved\":true}",
            "تأیید پیش‌فاکتور توسط مشتری",
            cancellationToken);
    }
}

public sealed class SubmitInstallationSpecsCommandHandler
    : IRequestHandler<SubmitInstallationSpecsCommand, Result<WorkflowInstanceDto>>
{
    private readonly BandwidthPhase6Support _support;

    public SubmitInstallationSpecsCommandHandler(BandwidthPhase6Support support)
    {
        _support = support;
    }

    public async Task<Result<WorkflowInstanceDto>> Handle(
        SubmitInstallationSpecsCommand request,
        CancellationToken cancellationToken)
    {
        var loaded = await _support.LoadAsync(request.InstanceId, cancellationToken);
        if (loaded is null)
        {
            return Result.Failure<WorkflowInstanceDto>("گردش کار یافت نشد.", "SALES_WORKFLOW_NOT_FOUND");
        }

        var (order, instance, definition) = loaded.Value;
        var form = await _support.SubmitFormAsync(
            definition.FindState(instance.CurrentStateId),
            order,
            request.DataJson,
            cancellationToken);
        if (form.IsFailure)
        {
            return Result.Failure<WorkflowInstanceDto>(form.Errors, form.Message);
        }

        return await _support.MoveAsync(
            instance,
            definition,
            "INSTALLATION_IN_PROGRESS",
            request.DataJson,
            "ثبت مشخصات نصب",
            cancellationToken);
    }
}

public sealed class IssueFinalInvoiceCommandHandler
    : IRequestHandler<IssueFinalInvoiceCommand, Result<ServiceDecisionResultDto>>
{
    private readonly BandwidthPhase6Support _support;
    private readonly IInvoiceRepository _invoices;
    private readonly IBillingUnitOfWork _billingUnitOfWork;
    private readonly ISequenceService _sequences;
    private readonly FoundationCurrentUser _currentUser;
    private readonly IDateTimeProvider _clock;

    public IssueFinalInvoiceCommandHandler(
        BandwidthPhase6Support support,
        IInvoiceRepository invoices,
        IBillingUnitOfWork billingUnitOfWork,
        ISequenceService sequences,
        FoundationCurrentUser currentUser,
        IDateTimeProvider clock)
    {
        _support = support;
        _invoices = invoices;
        _billingUnitOfWork = billingUnitOfWork;
        _sequences = sequences;
        _currentUser = currentUser;
        _clock = clock;
    }

    public async Task<Result<ServiceDecisionResultDto>> Handle(
        IssueFinalInvoiceCommand request,
        CancellationToken cancellationToken)
    {
        var loaded = await _support.LoadAsync(request.InstanceId, cancellationToken);
        if (loaded is null)
        {
            return Result.Failure<ServiceDecisionResultDto>("گردش کار یافت نشد.", "SALES_WORKFLOW_NOT_FOUND");
        }

        var (order, instance, definition) = loaded.Value;
        var item = order.Items.FirstOrDefault(item => item.ServiceId.HasValue);
        if (item is null)
        {
            return Result.Failure<ServiceDecisionResultDto>("آیتم سرویس یافت نشد.", "SALES_SERVICE_ITEM_NOT_FOUND");
        }

        var now = _clock.UtcNow;
        var number = await _sequences.GetNextValueAsync("InvoiceNumber", cancellationToken);
        var invoice = new Invoice(
            number,
            order.Id,
            order.CustomerId,
            request.InvoiceType,
            now,
            now.AddDays(7),
            0,
            _currentUser.UserId,
            now);
        invoice.AddItem(new InvoiceItem(invoice.Id, item.ItemName, 1, item.EffectivePrice, now));
        invoice.Issue(0, 0, _currentUser.UserId, now);
        _invoices.Add(invoice);
        await _billingUnitOfWork.SaveChangesAsync(cancellationToken);

        var moved = await _support.MoveAsync(
            instance,
            definition,
            request.RequiresContract ? "CONTRACT" : "PAYMENT_PENDING",
            "{\"invoiceId\":\"" + invoice.Id + "\"}",
            "صدور فاکتور نهایی",
            cancellationToken);

        return moved.IsFailure
            ? Result.Failure<ServiceDecisionResultDto>(moved.Errors, moved.Message)
            : Result.Success(new ServiceDecisionResultDto(moved.Value!, invoice.Id, item.EffectivePrice));
    }
}

public sealed class ActivateCustomerServiceCommandHandler
    : IRequestHandler<ActivateCustomerServiceCommand, Result<ServiceActivationResultDto>>
{
    private readonly IMediator _mediator;

    public ActivateCustomerServiceCommandHandler(IMediator mediator)
    {
        _mediator = mediator;
    }

    public Task<Result<ServiceActivationResultDto>> Handle(
        ActivateCustomerServiceCommand request,
        CancellationToken cancellationToken) =>
        _mediator.Send(new ActivateServiceCommand(request.OrderId), cancellationToken);
}

public sealed class CancelServiceRequestCommandHandler
    : IRequestHandler<CancelServiceRequestCommand, Result<WorkflowInstanceDto>>
{
    private readonly BandwidthPhase6Support _support;
    private readonly IOrderRepository _orders;
    private readonly IOrderUnitOfWork _unitOfWork;
    private readonly IMediator _mediator;
    private readonly FoundationCurrentUser _currentUser;
    private readonly IDateTimeProvider _clock;

    public CancelServiceRequestCommandHandler(
        BandwidthPhase6Support support,
        IOrderRepository orders,
        IOrderUnitOfWork unitOfWork,
        IMediator mediator,
        FoundationCurrentUser currentUser,
        IDateTimeProvider clock)
    {
        _support = support;
        _orders = orders;
        _unitOfWork = unitOfWork;
        _mediator = mediator;
        _currentUser = currentUser;
        _clock = clock;
    }

    public async Task<Result<WorkflowInstanceDto>> Handle(
        CancelServiceRequestCommand request,
        CancellationToken cancellationToken)
    {
        var order = await _orders.GetByIdWithDetailsAsync(request.OrderId, cancellationToken);
        if (order is null)
        {
            return Result.Failure<WorkflowInstanceDto>("سفارش یافت نشد.", "SALES_ORDER_NOT_FOUND");
        }

        var instance = await _support.LoadAsyncByOrderAsync(order.Id, cancellationToken);
        if (instance is not null)
        {
            await _mediator.Send(
                new CancelWorkflowInstanceCommand(instance.Id, request.Reason),
                cancellationToken);
        }

        if (instance is null)
        {
            return Result.Failure<WorkflowInstanceDto>(
                "گردش کار فعال برای این سفارش یافت نشد.",
                "SALES_WORKFLOW_NOT_ACTIVE");
        }

        var cancelled = await _mediator.Send(
            new CancelWorkflowInstanceCommand(instance.Id, request.Reason),
            cancellationToken);

        if (order.Status is not (OrderStatus.Canceled or OrderStatus.Completed)
            && Order.CanTransition(order.Status, OrderStatus.Canceled))
        {
            order.Cancel(_currentUser.UserId, _clock.UtcNow, request.Reason);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }

        var refreshed = await _mediator.Send(
            new GetWorkflowInstanceQuery(instance.Id),
            cancellationToken);
        if (refreshed.IsSuccess
            && refreshed.Value?.Status == nameof(WorkflowInstanceStatus.Cancelled))
        {
            return refreshed;
        }

        return cancelled.IsFailure
            ? Result.Failure<WorkflowInstanceDto>(cancelled.Errors, cancelled.Message)
            : Result.Success(cancelled.Value!);
    }
}

public sealed class CollectServiceCommandHandler
    : IRequestHandler<CollectServiceCommand, Result>
{
    private readonly ICustomerServiceRepository _customerServices;
    private readonly IOrderUnitOfWork _unitOfWork;
    private readonly FoundationCurrentUser _currentUser;
    private readonly IDateTimeProvider _clock;

    public CollectServiceCommandHandler(
        ICustomerServiceRepository customerServices,
        IOrderUnitOfWork unitOfWork,
        FoundationCurrentUser currentUser,
        IDateTimeProvider clock)
    {
        _customerServices = customerServices;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
        _clock = clock;
    }

    public async Task<Result> Handle(
        CollectServiceCommand request,
        CancellationToken cancellationToken)
    {
        var service = await _customerServices.GetByIdAsync(request.CustomerServiceId, cancellationToken);
        if (service is null)
        {
            return Result.Failure("سرویس فعال یافت نشد.", "SALES_CUSTOMER_SERVICE_NOT_FOUND");
        }

        service.Collect(_currentUser.UserId, _clock.UtcNow);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}

public sealed class AdjustBandwidthCommandHandler
    : IRequestHandler<AdjustBandwidthCommand, Result<ServiceChangeResultDto>>
{
    private readonly ICustomerServiceRepository _customerServices;
    private readonly IInvoiceRepository _invoices;
    private readonly IBillingUnitOfWork _billingUnitOfWork;
    private readonly ISequenceService _sequences;
    private readonly IOrderUnitOfWork _unitOfWork;
    private readonly IDateTimeProvider _clock;
    private readonly FoundationCurrentUser _currentUser;

    public AdjustBandwidthCommandHandler(
        ICustomerServiceRepository customerServices,
        IInvoiceRepository invoices,
        IBillingUnitOfWork billingUnitOfWork,
        ISequenceService sequences,
        IOrderUnitOfWork unitOfWork,
        IDateTimeProvider clock,
        FoundationCurrentUser currentUser)
    {
        _customerServices = customerServices;
        _invoices = invoices;
        _billingUnitOfWork = billingUnitOfWork;
        _sequences = sequences;
        _unitOfWork = unitOfWork;
        _clock = clock;
        _currentUser = currentUser;
    }

    public async Task<Result<ServiceChangeResultDto>> Handle(
        AdjustBandwidthCommand request,
        CancellationToken cancellationToken)
    {
        var service = await _customerServices.GetByIdAsync(request.CustomerServiceId, cancellationToken);
        if (service is null)
        {
            return Result.Failure<ServiceChangeResultDto>(
                "سرویس فعال یافت نشد.",
                "SALES_CUSTOMER_SERVICE_NOT_FOUND");
        }

        var oldQuantity = service.Quantity;
        if (request.NewQuantity <= 0)
        {
            return Result.Failure<ServiceChangeResultDto>(
                "پهنای باند جدید باید بزرگ‌تر از صفر باشد.",
                "SALES_BANDWIDTH_INVALID");
        }

        service.AdjustQuantity(request.NewQuantity, _currentUser.UserId, _clock.UtcNow);
        var difference = Math.Max(0, request.NewQuantity - oldQuantity) * service.UnitPriceSnapshot;
        Guid? invoiceId = null;
        if (difference > 0)
        {
            var now = _clock.UtcNow;
            var number = await _sequences.GetNextValueAsync("InvoiceNumber", cancellationToken);
            var invoice = new Invoice(
                number,
                service.OrderId,
                service.CustomerId,
                BillingInvoiceType.Proforma,
                now,
                now.AddDays(7),
                0,
                _currentUser.UserId,
                now);
            invoice.AddItem(new InvoiceItem(
                invoice.Id,
                $"افزایش پهنای باند {service.ServiceName}",
                1,
                difference,
                now));
            _invoices.Add(invoice);
            await _billingUnitOfWork.SaveChangesAsync(cancellationToken);
            invoiceId = invoice.Id;
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success(new ServiceChangeResultDto(service.Id, invoiceId, difference));
    }
}
