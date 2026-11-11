using System.Globalization;
using System.Text.Json;
using CrmSuite.BuildingBlocks.Application.Abstractions;
using CrmSuite.BuildingBlocks.Application.Security;
using CrmSuite.BuildingBlocks.Domain.Results;
using CrmSuite.Modules.Billing.Application.Abstractions;
using CrmSuite.Modules.Billing.Domain.Entities;
using CrmSuite.Modules.Billing.Domain.Enums;
using CrmSuite.Modules.Catalog.Application.Abstractions;
using CrmSuite.Modules.Catalog.Application.Services;
using CrmSuite.Modules.Catalog.Domain.Enums;
using CrmSuite.Modules.Customers.Application.Abstractions;
using CrmSuite.Modules.Foundation.Application.Abstractions;
using CrmSuite.Modules.Orders.Application.Abstractions;
using CrmSuite.Modules.Orders.Domain.Entities;
using CrmSuite.Modules.Orders.Domain.Enums;
using CrmSuite.Modules.Workflow.Application.Abstractions;
using CrmSuite.Modules.Workflow.Application.Commands;
using CrmSuite.Modules.Workflow.Application.Dtos;
using CrmSuite.Modules.Workflow.Application.Mapping;
using CrmSuite.Modules.Workflow.Domain.Entities;
using CrmSuite.Modules.Workflow.Domain.Enums;
using MediatR;
using FoundationCurrentUser = CrmSuite.Modules.Foundation.Application.Abstractions.ICurrentUserService;

namespace CrmSuite.Modules.Workflow.Application.Sales;

public class BandwidthWorkflowSupport
{
    private readonly IWorkflowInstanceRepository _instances;
    private readonly IWorkflowDefinitionRepository _definitions;
    private readonly IMediator _mediator;

    public BandwidthWorkflowSupport(
        IWorkflowInstanceRepository instances,
        IWorkflowDefinitionRepository definitions,
        IMediator mediator)
    {
        _instances = instances;
        _definitions = definitions;
        _mediator = mediator;
    }

    public virtual Task<WorkflowInstance?> GetInstanceAsync(
        Guid instanceId,
        CancellationToken cancellationToken) =>
        _instances.GetByIdWithGraphAsync(instanceId, cancellationToken);

    public virtual async Task<(WorkflowInstance Instance, WorkflowDefinition Definition)?> GetActiveAsync(
        Guid orderId,
        CancellationToken cancellationToken)
    {
        var instance = await _instances.GetActiveByEntityAsync(
            WorkflowEntityType.Order,
            orderId,
            cancellationToken);
        if (instance is null)
        {
            return null;
        }

        var definition = await _definitions.GetByIdWithGraphAsync(
            instance.DefinitionId,
            cancellationToken);
        return definition is null ? null : (instance, definition);
    }

    public virtual async Task<Result<WorkflowInstanceDto>> MoveAsync(
        WorkflowInstance instance,
        WorkflowDefinition definition,
        string targetStateCode,
        string? payloadJson,
        string? comment,
        CancellationToken cancellationToken)
    {
        var current = definition.FindState(instance.CurrentStateId);
        var target = definition.States.SingleOrDefault(
            state => string.Equals(state.Code, targetStateCode, StringComparison.OrdinalIgnoreCase));
        if (target is null)
        {
            return Result.Failure<WorkflowInstanceDto>(
                $"وضعیت «{targetStateCode}» در گردش کار یافت نشد.",
                "SALES_WORKFLOW_STATE_NOT_FOUND");
        }

        var transition = definition.Transitions.SingleOrDefault(
            item => item.FromStateId == current.Id && item.ToStateId == target.Id);
        if (transition is null)
        {
            return Result.Failure<WorkflowInstanceDto>(
                $"گذار از «{current.Name}» به «{target.Name}» در گردش کار تعریف نشده است.",
                "SALES_WORKFLOW_TRANSITION_NOT_FOUND");
        }

        return await _mediator.Send(
            new ExecuteWorkflowTransitionCommand(
                instance.Id,
                transition.Id,
                comment,
                payloadJson),
            cancellationToken);
    }
}

public sealed class CreateServiceRequestCommandHandler
    : IRequestHandler<CreateServiceRequestCommand, Result<ServiceRequestResultDto>>
{
    private readonly IOrderRepository _orders;
    private readonly IOrderUnitOfWork _orderUnitOfWork;
    private readonly ICustomerRepository _customers;
    private readonly IServiceRepository _services;
    private readonly IServicePricingService _pricing;
    private readonly ISequenceService _sequences;
    private readonly IWorkflowDefinitionRepository _definitions;
    private readonly IWorkflowStarter _workflowStarter;
    private readonly BandwidthWorkflowSupport _workflow;
    private readonly FoundationCurrentUser _currentUser;
    private readonly IDateTimeProvider _clock;

    public CreateServiceRequestCommandHandler(
        IOrderRepository orders,
        IOrderUnitOfWork orderUnitOfWork,
        ICustomerRepository customers,
        IServiceRepository services,
        IServicePricingService pricing,
        ISequenceService sequences,
        IWorkflowDefinitionRepository definitions,
        IWorkflowStarter workflowStarter,
        BandwidthWorkflowSupport workflow,
        FoundationCurrentUser currentUser,
        IDateTimeProvider clock)
    {
        _workflow = workflow;
        _orders = orders;
        _orderUnitOfWork = orderUnitOfWork;
        _customers = customers;
        _services = services;
        _pricing = pricing;
        _sequences = sequences;
        _definitions = definitions;
        _workflowStarter = workflowStarter;
        _currentUser = currentUser;
        _clock = clock;
    }

    public async Task<Result<ServiceRequestResultDto>> Handle(
        CreateServiceRequestCommand request,
        CancellationToken cancellationToken)
    {
        if (_currentUser.UserId is null)
        {
            return Result.Failure<ServiceRequestResultDto>(
                "کارشناس فروش جاری قابل تشخیص نیست.",
                "SALES_USER_REQUIRED");
        }

        var customer = await _customers.GetByIdAsync(
            request.CustomerId,
            includeDetails: false,
            cancellationToken);
        if (customer is null)
        {
            return Result.Failure<ServiceRequestResultDto>(
                "مشتری یافت نشد.",
                "SALES_CUSTOMER_NOT_FOUND");
        }

        var service = await _services.GetByIdAsync(request.ServiceId, cancellationToken);
        if (service is null || !service.IsActive)
        {
            return Result.Failure<ServiceRequestResultDto>(
                "سرویس یافت نشد یا فعال نیست.",
                "SALES_SERVICE_NOT_FOUND");
        }

        var attributes = string.IsNullOrWhiteSpace(request.AttributesJson)
            ? "{}"
            : request.AttributesJson;
        var substrate = ReadString(attributes, "substrate", "medium", "connection_type");
        var bandwidth = ReadDecimal(attributes, "bandwidth_mb", "bandwidth");
        var requiresFeasibility = request.RequiresFeasibility
                                  || substrate.Contains("wireless", StringComparison.OrdinalIgnoreCase)
                                  || substrate.Contains("وایرلس", StringComparison.Ordinal);
        var quantity = service.ServiceType is ServiceType.BandwidthDedicated or ServiceType.BandwidthShared
                       && bandwidth.HasValue
            ? Math.Max(1, (int)Math.Ceiling(bandwidth.Value))
            : 1;

        var calculated = await _pricing.CalculateAsync(
            new ServicePricingRequest(service.Id, quantity, [], attributes),
            cancellationToken);
        if (calculated.IsFailure)
        {
            return Result.Failure<ServiceRequestResultDto>(
                calculated.Errors,
                calculated.Message ?? "محاسبه قیمت سرویس ناموفق بود.");
        }

        var now = _clock.UtcNow;
        string orderNumber;
        try
        {
            orderNumber = await _sequences.GetNextValueAsync("OrderNumber", cancellationToken);
        }
        catch (Exception exception)
        {
            return Result.Failure<ServiceRequestResultDto>(
                exception.Message,
                "SALES_ORDER_NUMBER_FAILED");
        }

        var order = new Order(
            orderNumber,
            customer.Id,
            _currentUser.UserId.Value,
            now,
            OrderType.New,
            "درخواست سرویس جدید",
            _currentUser.UserId,
            now);

        var billingCycle = service.BillingPeriod switch
        {
            BillingPeriod.Monthly => BillingCycle.Monthly,
            BillingPeriod.Yearly => BillingCycle.Yearly,
            _ => BillingCycle.OneTime,
        };

        order.AddItem(new OrderItem(
            order.Id,
            service.Id,
            service.Name,
            quantity,
            calculated.Value!.UnitPriceSnapshot,
            calculated.Value.AddonsTotal,
            calculated.Value.AddonsJson,
            attributes,
            billingCycle,
            now,
            _currentUser.UserId));
        order.ChangeStatus(OrderStatus.Pending, _currentUser.UserId, now, "ثبت درخواست سرویس");

        _orders.Add(order);
        await _orderUnitOfWork.SaveChangesAsync(cancellationToken);

        var definitionCode = await ResolveWorkflowCodeAsync(
            service.WorkflowDefinitionId,
            requiresFeasibility,
            cancellationToken);
        if (definitionCode is null)
        {
            order.ChangeStatus(OrderStatus.Canceled, _currentUser.UserId, now, "گردش کار مناسب یافت نشد.");
            await _orderUnitOfWork.SaveChangesAsync(cancellationToken);
            return Result.Failure<ServiceRequestResultDto>(
                "گردش کار مناسب برای این سرویس فعال و منتشر نشده است.",
                "SALES_WORKFLOW_NOT_FOUND");
        }

        var workflow = await _workflowStarter.TryStartAsync(
            WorkflowEntityType.Order,
            order.Id,
            definitionCode,
            JsonSerializer.Serialize(new
            {
                Source = "ServiceRequest",
                serviceId = service.Id,
                requiresFeasibility,
                substrate,
                attributes = TryParse(attributes),
            }),
            $"service-request:{order.Id:N}",
            cancellationToken);

        if (workflow is null)
        {
            order.ChangeStatus(OrderStatus.Canceled, _currentUser.UserId, now, "شروع گردش کار ناموفق بود.");
            await _orderUnitOfWork.SaveChangesAsync(cancellationToken);
            return Result.Failure<ServiceRequestResultDto>(
                "گردش کار سرویس آغاز نشد؛ تعریف منتشرشده را بررسی کنید.",
                "SALES_WORKFLOW_START_FAILED");
        }

        var started = await _workflow.GetActiveAsync(order.Id, cancellationToken);
        if (started is not null)
        {
            var firstTarget = requiresFeasibility ? "FEASIBILITY_TRIAGE" : "EQUIPMENT_SUPPLY";
            var firstMove = await _workflow.MoveAsync(
                started.Value.Instance,
                started.Value.Definition,
                firstTarget,
                JsonSerializer.Serialize(new { startedAt = now, source = "ServiceRequest" }),
                "شروع خودکار مسیر سرویس",
                cancellationToken);
            if (firstMove.IsFailure)
            {
                return Result.Failure<ServiceRequestResultDto>(
                    firstMove.Errors,
                    firstMove.Message);
            }

            workflow = firstMove.Value;
        }

        return Result.Success(new ServiceRequestResultDto(
            order.Id,
            order.OrderNumber,
            workflow!.Id,
            workflow.DefinitionCode,
            order.TotalAmount,
            requiresFeasibility));
    }

    private async Task<string?> ResolveWorkflowCodeAsync(
        Guid? serviceWorkflowId,
        bool requiresFeasibility,
        CancellationToken cancellationToken)
    {
        if (serviceWorkflowId.HasValue)
        {
            var definition = await _definitions.GetByIdWithGraphAsync(
                serviceWorkflowId.Value,
                cancellationToken);
            if (definition is { Status: WorkflowDefinitionStatus.Published })
            {
                return definition.Code;
            }
        }

        return requiresFeasibility
            ? BandwidthWorkflowCodes.WithFeasibility
            : BandwidthWorkflowCodes.NoFeasibility;
    }

    private static string ReadString(string json, params string[] keys)
    {
        using var document = JsonDocument.Parse(json);
        foreach (var key in keys)
        {
            if (document.RootElement.TryGetProperty(key, out var value))
            {
                return value.ValueKind == JsonValueKind.String
                    ? value.GetString() ?? string.Empty
                    : value.ToString();
            }
        }

        return string.Empty;
    }

    private static decimal? ReadDecimal(string json, params string[] keys)
    {
        using var document = JsonDocument.Parse(json);
        foreach (var key in keys)
        {
            if (!document.RootElement.TryGetProperty(key, out var value))
            {
                continue;
            }

            if (value.ValueKind == JsonValueKind.Number && value.TryGetDecimal(out var number))
            {
                return number;
            }

            if (decimal.TryParse(value.ToString(), NumberStyles.Number, CultureInfo.InvariantCulture, out var parsed))
            {
                return parsed;
            }
        }

        return null;
    }

    private static object? TryParse(string json)
    {
        try
        {
            return JsonSerializer.Deserialize<object>(json);
        }
        catch (JsonException)
        {
            return null;
        }
    }
}

public sealed class SubmitFeasibilityReportCommandHandler
    : IRequestHandler<SubmitFeasibilityReportCommand, Result<WorkflowInstanceDto>>
{
    private readonly BandwidthWorkflowSupport _workflow;
    private readonly IOrderRepository _orders;
    private readonly IOrderUnitOfWork _unitOfWork;
    private readonly IDateTimeProvider _clock;
    private readonly FoundationCurrentUser _currentUser;

    public SubmitFeasibilityReportCommandHandler(
        BandwidthWorkflowSupport workflow,
        IOrderRepository orders,
        IOrderUnitOfWork unitOfWork,
        IDateTimeProvider clock,
        FoundationCurrentUser currentUser)
    {
        _workflow = workflow;
        _orders = orders;
        _unitOfWork = unitOfWork;
        _clock = clock;
        _currentUser = currentUser;
    }

    public async Task<Result<WorkflowInstanceDto>> Handle(
        SubmitFeasibilityReportCommand request,
        CancellationToken cancellationToken)
    {
        var instance = await _workflow.GetInstanceAsync(request.InstanceId, cancellationToken);
        if (instance is null)
        {
            return Result.Failure<WorkflowInstanceDto>(
                "نمونه گردش کار یافت نشد.",
                "SALES_WORKFLOW_NOT_FOUND");
        }

        var active = await _workflow.GetActiveAsync(instance.EntityId, cancellationToken);
        if (active is null)
        {
            return Result.Failure<WorkflowInstanceDto>(
                "گردش کار فعال برای این درخواست یافت نشد.",
                "SALES_WORKFLOW_NOT_ACTIVE");
        }

        if (string.Equals(
                active.Value.Definition.FindState(active.Value.Instance.CurrentStateId).Code,
                "START",
                StringComparison.OrdinalIgnoreCase))
        {
            var toFeasibility = await _workflow.MoveAsync(
                active.Value.Instance,
                active.Value.Definition,
                "FEASIBILITY",
                null,
                "شروع امکان‌سنجی",
                cancellationToken);
            if (toFeasibility.IsFailure)
            {
                return toFeasibility;
            }

            active = await _workflow.GetActiveAsync(instance.EntityId, cancellationToken);
            if (active is null)
            {
                return Result.Failure<WorkflowInstanceDto>(
                    "گردش کار فعال یافت نشد.",
                    "SALES_WORKFLOW_NOT_ACTIVE");
            }
        }

        var result = await _workflow.MoveAsync(
            active.Value.Instance,
            active.Value.Definition,
            "BACK_TO_SALES",
            request.ReportJson,
            "ثبت گزارش امکان‌سنجی",
            cancellationToken);
        if (result.IsSuccess)
        {
            await SetOrderStatusAsync(active.Value.Instance.EntityId, OrderStatus.Provisioning, cancellationToken);
        }

        return result;
    }

    private async Task SetOrderStatusAsync(
        Guid orderId,
        OrderStatus status,
        CancellationToken cancellationToken)
    {
        var order = await _orders.GetByIdWithDetailsAsync(orderId, cancellationToken);
        if (order is null || order.Status == status)
        {
            return;
        }

        if (Order.CanTransition(order.Status, status))
        {
            order.ChangeStatus(status, _currentUser.UserId, _clock.UtcNow, "همگام‌سازی با گردش کار");
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
    }
}

public sealed class DecidePurchaseOrInvoiceCommandHandler
    : IRequestHandler<DecidePurchaseOrInvoiceCommand, Result<ServiceDecisionResultDto>>
{
    private readonly BandwidthWorkflowSupport _workflow;
    private readonly IOrderRepository _orders;
    private readonly IOrderUnitOfWork _unitOfWork;
    private readonly IInvoiceRepository _invoices;
    private readonly IBillingUnitOfWork _billingUnitOfWork;
    private readonly ISequenceService _sequences;
    private readonly IDateTimeProvider _clock;
    private readonly FoundationCurrentUser _currentUser;
    private readonly WorkflowDtoMapper _mapper;

    public DecidePurchaseOrInvoiceCommandHandler(
        BandwidthWorkflowSupport workflow,
        IOrderRepository orders,
        IOrderUnitOfWork unitOfWork,
        IInvoiceRepository invoices,
        IBillingUnitOfWork billingUnitOfWork,
        ISequenceService sequences,
        IDateTimeProvider clock,
        FoundationCurrentUser currentUser,
        WorkflowDtoMapper mapper)
    {
        _workflow = workflow;
        _orders = orders;
        _unitOfWork = unitOfWork;
        _invoices = invoices;
        _billingUnitOfWork = billingUnitOfWork;
        _sequences = sequences;
        _clock = clock;
        _currentUser = currentUser;
        _mapper = mapper;
    }

    public async Task<Result<ServiceDecisionResultDto>> Handle(
        DecidePurchaseOrInvoiceCommand request,
        CancellationToken cancellationToken)
    {
        var instance = await _workflow.GetInstanceAsync(request.InstanceId, cancellationToken);
        if (instance is null)
        {
            return Result.Failure<ServiceDecisionResultDto>(
                "نمونه گردش کار یافت نشد.",
                "SALES_WORKFLOW_NOT_FOUND");
        }

        var active = await _workflow.GetActiveAsync(instance.EntityId, cancellationToken);
        if (active is null)
        {
            return Result.Failure<ServiceDecisionResultDto>(
                "گردش کار فعال برای این سفارش یافت نشد.",
                "SALES_WORKFLOW_NOT_ACTIVE");
        }

        var currentCode = active.Value.Definition
            .FindState(active.Value.Instance.CurrentStateId)
            .Code;
        var alreadyAtProforma = string.Equals(
            currentCode,
            "PROFORMA",
            StringComparison.OrdinalIgnoreCase);

        WorkflowInstanceDto workflowDto;
        if (!(alreadyAtProforma
              && request.Decision == PurchaseOrInvoiceDecision.IssueProforma))
        {
            var target = request.Decision == PurchaseOrInvoiceDecision.PurchaseEquipment
                ? "PURCHASE"
                : "PROFORMA";
            var result = await _workflow.MoveAsync(
                active.Value.Instance,
                active.Value.Definition,
                target,
                JsonSerializer.Serialize(new { decision = request.Decision.ToString() }),
                request.Decision.ToString(),
                cancellationToken);
            if (result.IsFailure)
            {
                return Result.Failure<ServiceDecisionResultDto>(result.Errors, result.Message);
            }

            workflowDto = result.Value!;
        }
        else
        {
            workflowDto = _mapper.ToInstance(
                active.Value.Instance,
                active.Value.Definition);
        }

        Guid? invoiceId = null;
        var amount = 0m;
        if (request.Decision == PurchaseOrInvoiceDecision.IssueProforma)
        {
            var proforma = await CreateProformaAsync(instance.EntityId, cancellationToken);
            if (proforma.IsFailure)
            {
                return Result.Failure<ServiceDecisionResultDto>(proforma.Errors, proforma.Message);
            }

            invoiceId = proforma.Value!.InvoiceId;
            amount = proforma.Value.Amount;
        }

        return Result.Success(new ServiceDecisionResultDto(workflowDto, invoiceId, amount));
    }

    internal async Task<Result<(Guid InvoiceId, decimal Amount)>> CreateProformaAsync(
        Guid orderId,
        CancellationToken cancellationToken)
    {
        var order = await _orders.GetByIdWithDetailsAsync(orderId, cancellationToken);
        if (order is null || order.Items.Count == 0)
        {
            return Result.Failure<(Guid InvoiceId, decimal Amount)>(
                "سفارش یا آیتم سرویس یافت نشد.",
                "SALES_ORDER_NOT_FOUND");
        }

        var item = order.Items.First();
        var now = _clock.UtcNow;
        var number = await _sequences.GetNextValueAsync("InvoiceNumber", cancellationToken);
        var invoice = new Invoice(
            number,
            order.Id,
            order.CustomerId,
            BillingInvoiceType.Proforma,
            now,
            now.AddDays(7),
            0,
            _currentUser.UserId,
            now);
        invoice.AddItem(new InvoiceItem(invoice.Id, item.ItemName, 1, item.EffectivePrice, now));
        _invoices.Add(invoice);
        await _billingUnitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success((invoice.Id, item.EffectivePrice));
    }
}

public sealed class SubmitInstallationReportCommandHandler
    : IRequestHandler<SubmitInstallationReportCommand, Result<ServiceDecisionResultDto>>
{
    private readonly BandwidthPhase6Support _support;
    private readonly IOrderRepository _orders;

    public SubmitInstallationReportCommandHandler(
        BandwidthPhase6Support support,
        IOrderRepository orders)
    {
        _support = support;
        _orders = orders;
    }

    public async Task<Result<ServiceDecisionResultDto>> Handle(
        SubmitInstallationReportCommand request,
        CancellationToken cancellationToken)
    {
        var loaded = await _support.LoadAsync(request.InstanceId, cancellationToken);
        if (loaded is null)
        {
            return Result.Failure<ServiceDecisionResultDto>(
                "گردش کار یافت نشد.",
                "SALES_WORKFLOW_NOT_FOUND");
        }

        var (order, instance, definition) = loaded.Value;
        var state = definition.FindState(instance.CurrentStateId);
        var form = await _support.SubmitFormAsync(
            state,
            order,
            request.UsedItemsJson,
            cancellationToken);
        if (form.IsFailure)
        {
            return Result.Failure<ServiceDecisionResultDto>(form.Errors, form.Message);
        }

        var transition = await _support.MoveAsync(
            instance,
            definition,
            "FINAL_INVOICE",
            request.UsedItemsJson,
            "ثبت گزارش نصب",
            cancellationToken);
        return transition.IsFailure
            ? Result.Failure<ServiceDecisionResultDto>(transition.Errors, transition.Message)
            : Result.Success(new ServiceDecisionResultDto(transition.Value!, null, 0m));
    }
}

public sealed class ActivateServiceCommandHandler
    : IRequestHandler<ActivateServiceCommand, Result<ServiceActivationResultDto>>
{
    private readonly IOrderRepository _orders;
    private readonly ICustomerServiceRepository _customerServices;
    private readonly IServiceRepository _services;
    private readonly IOrderUnitOfWork _unitOfWork;
    private readonly BandwidthWorkflowSupport _workflow;
    private readonly IDateTimeProvider _clock;
    private readonly FoundationCurrentUser _currentUser;
    private readonly IMediator? _mediator;
    private readonly IInvoiceRepository? _invoices;

    public ActivateServiceCommandHandler(
        IOrderRepository orders,
        ICustomerServiceRepository customerServices,
        IServiceRepository services,
        IOrderUnitOfWork unitOfWork,
        BandwidthWorkflowSupport workflow,
        IDateTimeProvider clock,
        FoundationCurrentUser currentUser,
        IMediator? mediator = null,
        IInvoiceRepository? invoices = null)
    {
        _orders = orders;
        _customerServices = customerServices;
        _services = services;
        _unitOfWork = unitOfWork;
        _workflow = workflow;
        _clock = clock;
        _currentUser = currentUser;
        _mediator = mediator;
        _invoices = invoices;
    }

    public async Task<Result<ServiceActivationResultDto>> Handle(
        ActivateServiceCommand request,
        CancellationToken cancellationToken)
    {
        var order = await _orders.GetByIdWithDetailsAsync(request.OrderId, cancellationToken);
        if (order is null)
        {
            return Result.Failure<ServiceActivationResultDto>("سفارش یافت نشد.", "SALES_ORDER_NOT_FOUND");
        }

        var existing = await _customerServices.GetByOrderIdAsync(order.Id, cancellationToken);
        if (existing is not null)
        {
            return Result.Success(new ServiceActivationResultDto(
                order.Id,
                existing.Id,
                order.WorkflowInstanceId,
                existing.ActivatedAt,
                existing.CommissionBase));
        }

        Invoice? paidInvoice = null;
        if (_invoices is not null)
        {
            var invoices = await _invoices.GetByOrderAsync(order.Id, cancellationToken);
            paidInvoice = invoices
                .Where(invoice => invoice.Status == InvoiceStatus.Paid)
                .OrderByDescending(invoice => invoice.CreatedAt)
                .FirstOrDefault();
            if (paidInvoice is null)
            {
                return Result.Failure<ServiceActivationResultDto>(
                    "فعال‌سازی فقط با فاکتور پرداخت‌شده مجاز است.",
                    "SALES_INVOICE_NOT_PAID");
            }
        }

        var active = await _workflow.GetActiveAsync(order.Id, cancellationToken);
        if (active is not null)
        {
            var currentCode = active.Value.Definition.FindState(active.Value.Instance.CurrentStateId).Code;
            if (!string.Equals(currentCode, "ACTIVATED", StringComparison.OrdinalIgnoreCase)
                && !string.Equals(currentCode, "COMPLETED", StringComparison.OrdinalIgnoreCase))
            {
                if (!string.Equals(currentCode, "PAYMENT", StringComparison.OrdinalIgnoreCase)
                    && !string.Equals(currentCode, "PAYMENT_PENDING", StringComparison.OrdinalIgnoreCase)
                    && !string.Equals(currentCode, "CONTRACT", StringComparison.OrdinalIgnoreCase))
                {
                    return Result.Failure<ServiceActivationResultDto>(
                        "فعال‌سازی فقط پس از تأیید پرداخت یا قرارداد مجاز است.",
                        "SALES_PAYMENT_REQUIRED");
                }

                var targetState = string.Equals(
                    currentCode,
                    "PAYMENT",
                    StringComparison.OrdinalIgnoreCase)
                    ? "ACTIVATED"
                    : "COMPLETED";
                var transition = await _workflow.MoveAsync(
                    active.Value.Instance,
                    active.Value.Definition,
                    targetState,
                    "{\"action\":\"activate\"}",
                    "تأیید پرداخت و فعال‌سازی",
                    cancellationToken);
                if (transition.IsFailure)
                {
                    return Result.Failure<ServiceActivationResultDto>(
                        transition.Errors,
                        transition.Message);
                }
            }
        }


        var item = order.Items.FirstOrDefault(item => item.ServiceId.HasValue);
        if (item is null)
        {
            return Result.Failure<ServiceActivationResultDto>(
                "سفارش آیتم سرویس قابل فعال‌سازی ندارد.",
                "SALES_SERVICE_ITEM_NOT_FOUND");
        }

        var now = _clock.UtcNow;
        order.Activate(_currentUser.UserId, now);
        var service = await _services.GetByIdAsync(item.ServiceId!.Value, cancellationToken);
        var customerService = new CustomerService(
            order.CustomerId,
            item.ServiceId.Value,
            order.Id,
            item.Id,
            item.ItemName,
            item.UnitPriceSnapshot,
            item.Quantity,
            item.AddonsTotal,
            item.AttributesJson,
            service?.CommissionPercent,
            now,
            now.AddMonths(1),
            _currentUser.UserId);
        _customerServices.Add(customerService);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        if (_mediator is not null)
        {
            await _mediator.Publish(
                new Orders.Application.Notifications.ServiceActivatedEvent(
                    order.Id,
                    item.Id,
                    order.CustomerId,
                    customerService.Id,
                    order.SalesExpertId,
                    paidInvoice?.TotalAmount ?? item.CommissionBase,
                    service?.CommissionPercent,
                    item.ItemName),
                cancellationToken);
        }

        return Result.Success(new ServiceActivationResultDto(
            order.Id,
            customerService.Id,
            order.WorkflowInstanceId,
            customerService.ActivatedAt,
            customerService.CommissionBase));
    }
}

public sealed class ConfirmServicePaymentCommandHandler
    : IRequestHandler<ConfirmServicePaymentCommand, Result<ServiceActivationResultDto>>
{
    private readonly IInvoiceRepository _invoices;
    private readonly IBillingUnitOfWork _billingUnitOfWork;
    private readonly ISettingService _settings;
    private readonly IMediator _mediator;
    private readonly IDateTimeProvider _clock;
    private readonly FoundationCurrentUser _currentUser;

    public ConfirmServicePaymentCommandHandler(
        IInvoiceRepository invoices,
        IBillingUnitOfWork billingUnitOfWork,
        ISettingService settings,
        IMediator mediator,
        IDateTimeProvider clock,
        FoundationCurrentUser currentUser)
    {
        _invoices = invoices;
        _billingUnitOfWork = billingUnitOfWork;
        _settings = settings;
        _mediator = mediator;
        _clock = clock;
        _currentUser = currentUser;
    }

    public async Task<Result<ServiceActivationResultDto>> Handle(
        ConfirmServicePaymentCommand request,
        CancellationToken cancellationToken)
    {
        var invoice = await _invoices.GetByIdWithItemsAsync(request.InvoiceId, cancellationToken);
        if (invoice is null)
        {
            return Result.Failure<ServiceActivationResultDto>("فاکتور یافت نشد.", "SALES_INVOICE_NOT_FOUND");
        }

        var now = _clock.UtcNow;
        try
        {
            if (invoice.Status == InvoiceStatus.Draft)
            {
                invoice.Issue(
                    await ReadPercentageAsync("Billing.VatPercentage", cancellationToken),
                    await ReadPercentageAsync("Billing.InsurancePercentage", cancellationToken),
                    _currentUser.UserId,
                    now);
            }

            if (invoice.Status != InvoiceStatus.Paid)
            {
                invoice.RegisterPayment(
                    invoice.TotalAmount - invoice.PaidAmount,
                    _currentUser.UserId,
                    now);
            }

            await _billingUnitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (CrmSuite.BuildingBlocks.Domain.Exceptions.DomainException exception)
        {
            return Result.Failure<ServiceActivationResultDto>(exception.Message, exception.Code);
        }

        var activation = await _mediator.Send(
            new ActivateServiceCommand(invoice.OrderId),
            cancellationToken);
        return activation;
    }

    private async Task<decimal> ReadPercentageAsync(
        string key,
        CancellationToken cancellationToken)
    {
        var setting = await _settings.GetAsync(key, false, cancellationToken);
        return decimal.TryParse(
            setting?.Value,
            NumberStyles.Number,
            CultureInfo.InvariantCulture,
            out var value)
            ? value
            : 0m;
    }
}

public sealed class SetPaymentModeCommandHandler
    : IRequestHandler<SetPaymentModeCommand, Result<WorkflowInstanceDto>>
{
    private readonly BandwidthWorkflowSupport _workflow;
    private readonly IOrderRepository _orders;
    private readonly IOrderUnitOfWork _unitOfWork;
    private readonly IDateTimeProvider _clock;
    private readonly FoundationCurrentUser _currentUser;

    public SetPaymentModeCommandHandler(
        BandwidthWorkflowSupport workflow,
        IOrderRepository orders,
        IOrderUnitOfWork unitOfWork,
        IDateTimeProvider clock,
        FoundationCurrentUser currentUser)
    {
        _workflow = workflow;
        _orders = orders;
        _unitOfWork = unitOfWork;
        _clock = clock;
        _currentUser = currentUser;
    }

    public async Task<Result<WorkflowInstanceDto>> Handle(
        SetPaymentModeCommand request,
        CancellationToken cancellationToken)
    {
        var instance = await _workflow.GetInstanceAsync(request.InstanceId, cancellationToken);
        if (instance is null)
        {
            return Result.Failure<WorkflowInstanceDto>("سفارش مرتبط با گردش کار یافت نشد.", "SALES_ORDER_NOT_FOUND");
        }

        var orderId = instance.EntityId;
        var active = await _workflow.GetActiveAsync(orderId, cancellationToken);
        if (active is null)
        {
            return Result.Failure<WorkflowInstanceDto>("گردش کار فعال یافت نشد.", "SALES_WORKFLOW_NOT_ACTIVE");
        }

        var currentCode = active.Value.Definition.FindState(active.Value.Instance.CurrentStateId).Code;
        if (string.Equals(currentCode, "PROFORMA", StringComparison.OrdinalIgnoreCase))
        {
            var toPaymentMode = await _workflow.MoveAsync(
                active.Value.Instance,
                active.Value.Definition,
                "PAYMENT_MODE",
                null,
                "پیش‌فاکتور صادر شد",
                cancellationToken);
            if (toPaymentMode.IsFailure)
            {
                return toPaymentMode;
            }

            active = await _workflow.GetActiveAsync(orderId, cancellationToken);
            if (active is null)
            {
                return Result.Failure<WorkflowInstanceDto>(
                    "گردش کار فعال یافت نشد.",
                    "SALES_WORKFLOW_NOT_ACTIVE");
            }
        }

        var target = request.PaymentMode == ServicePaymentMode.AfterInstallation
            ? "INSTALLATION"
            : "PAYMENT";
        var result = await _workflow.MoveAsync(
            active.Value.Instance,
            active.Value.Definition,
            target,
            JsonSerializer.Serialize(new { paymentMode = request.PaymentMode.ToString() }),
            request.PaymentMode.ToString(),
            cancellationToken);
        if (result.IsFailure)
        {
            return result;
        }

        await SetStatusAsync(orderId, request.PaymentMode == ServicePaymentMode.AfterInstallation
            ? OrderStatus.Provisioning
            : OrderStatus.PendingPayment, cancellationToken);
        return result;
    }

    private async Task SetStatusAsync(Guid orderId, OrderStatus status, CancellationToken cancellationToken)
    {
        var order = await _orders.GetByIdWithDetailsAsync(orderId, cancellationToken);
        if (order is null || order.Status == status || !Order.CanTransition(order.Status, status))
        {
            return;
        }

        order.ChangeStatus(status, _currentUser.UserId, _clock.UtcNow, "همگام‌سازی با گردش کار");
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
