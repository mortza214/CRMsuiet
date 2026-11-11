using CrmSuite.BuildingBlocks.Domain.Results;
using CrmSuite.Modules.Customers.Application.Abstractions;
using CrmSuite.Modules.Foundation.Application.Abstractions;
using CrmSuite.Modules.Orders.Application.Abstractions;
using CrmSuite.Modules.Orders.Application.Dtos;
using CrmSuite.Modules.Orders.Application.Queries;
using CrmSuite.Modules.Orders.Domain.Entities;
using CrmSuite.Modules.Orders.Domain.Enums;
using CrmSuite.Modules.Workflow.Application.Abstractions;
using CrmSuite.Modules.Workflow.Domain.Entities;
using CrmSuite.Modules.Workflow.Domain.Enums;
using MediatR;
using FoundationCurrentUser = CrmSuite.Modules.Foundation.Application.Abstractions.ICurrentUserService;

namespace CrmSuite.Modules.Workflow.Application.Sales;

public sealed record ServiceRequestSummaryDto(
    Guid OrderId,
    string OrderNumber,
    Guid CustomerId,
    string? CustomerName,
    string? ServiceName,
    decimal TotalAmount,
    string OrderStatus,
    DateTime CreatedAt,
    string CreatedAtJalali,
    Guid? WorkflowInstanceId,
    string? WorkflowCode,
    string? WorkflowStateCode,
    string? WorkflowStateName,
    string? WorkflowStatus,
    Guid? SalesExpertId = null);

public sealed record ServiceRequestDetailDto(
    Guid OrderId,
    string OrderNumber,
    Guid CustomerId,
    string? CustomerName,
    string? CustomerMobile,
    Guid? ServiceId,
    string? ServiceName,
    int Quantity,
    decimal UnitPriceSnapshot,
    decimal AddonsTotal,
    decimal EffectivePrice,
    string? AttributesJson,
    string OrderStatus,
    DateTime CreatedAt,
    string CreatedAtJalali,
    Guid? WorkflowInstanceId,
    string? WorkflowCode,
    string? WorkflowStateCode,
    string? WorkflowStateName,
    string? WorkflowStatus,
    IReadOnlyList<ServiceRequestTaskDto> Tasks);

public sealed record ServiceRequestTaskDto(
    Guid Id,
    string StateName,
    string Status,
    Guid? AssignedUserId,
    Guid? AssignedDepartmentId,
    DateTime CreatedAt,
    string CreatedAtJalali);

public sealed record GetServiceRequestDetailsQuery(Guid OrderId)
    : IRequest<Result<ServiceRequestDetailDto>>;

public sealed record GetCustomerServiceRequestsQuery(Guid CustomerId, int Limit = 20)
    : IRequest<Result<IReadOnlyList<ServiceRequestSummaryDto>>>;

public sealed record GetRecentServiceRequestsQuery(Guid? SalesExpertId = null, int Limit = 10)
    : IRequest<Result<IReadOnlyList<ServiceRequestSummaryDto>>>;

public sealed record GetPortalServiceRequestsQuery
    : IRequest<Result<IReadOnlyList<ServiceRequestSummaryDto>>>;

public sealed record GetPortalCustomerServicesQuery
    : IRequest<Result<IReadOnlyList<CustomerServiceDto>>>;

internal static class ServiceRequestQuerySupport
{
    public static async Task<(WorkflowInstance? Instance, WorkflowDefinition? Definition)> FindWorkflowAsync(
        Guid orderId,
        IWorkflowInstanceRepository instances,
        IWorkflowDefinitionRepository definitions,
        CancellationToken cancellationToken)
    {
        var instance = await instances.GetActiveByEntityAsync(
            WorkflowEntityType.Order,
            orderId,
            cancellationToken);
        if (instance is null)
        {
            var history = await instances.GetByEntityAsync(
                WorkflowEntityType.Order,
                orderId,
                cancellationToken);
            instance = history.Count > 0 ? history[0] : null;
        }

        if (instance is null)
        {
            return (null, null);
        }

        var definition = await definitions.GetByIdWithGraphAsync(
            instance.DefinitionId,
            cancellationToken);
        return (instance, definition);
    }

    public static string? StateCode(
        WorkflowInstance? instance,
        WorkflowDefinition? definition) =>
        instance is null || definition is null
            ? null
            : definition.FindState(instance.CurrentStateId).Code;

    public static string? StateName(
        WorkflowInstance? instance,
        WorkflowDefinition? definition) =>
        instance is null || definition is null
            ? null
            : definition.FindState(instance.CurrentStateId).Name;
}

public sealed class GetServiceRequestDetailsQueryHandler
    : IRequestHandler<GetServiceRequestDetailsQuery, Result<ServiceRequestDetailDto>>
{
    private readonly IOrderRepository _orders;
    private readonly IWorkflowInstanceRepository _instances;
    private readonly IWorkflowDefinitionRepository _definitions;
    private readonly ICustomerRepository _customers;
    private readonly IJalaliDateService _jalali;

    public GetServiceRequestDetailsQueryHandler(
        IOrderRepository orders,
        IWorkflowInstanceRepository instances,
        IWorkflowDefinitionRepository definitions,
        ICustomerRepository customers,
        IJalaliDateService jalali)
    {
        _orders = orders;
        _instances = instances;
        _definitions = definitions;
        _customers = customers;
        _jalali = jalali;
    }

    public async Task<Result<ServiceRequestDetailDto>> Handle(
        GetServiceRequestDetailsQuery request,
        CancellationToken cancellationToken)
    {
        var order = await _orders.GetByIdWithDetailsAsync(request.OrderId, cancellationToken);
        if (order is null)
        {
            return Result.Failure<ServiceRequestDetailDto>("سفارش یافت نشد.", "SALES_ORDER_NOT_FOUND");
        }

        var customer = await _customers.GetByIdAsync(
            order.CustomerId,
            includeDetails: false,
            cancellationToken);
        var item = order.Items.FirstOrDefault(item => item.ServiceId.HasValue);
        var workflow = await ServiceRequestQuerySupport.FindWorkflowAsync(
            order.Id,
            _instances,
            _definitions,
            cancellationToken);

        var tasks = workflow.Item1?.Tasks
            .OrderBy(task => task.CreatedAt)
            .Select(task => new ServiceRequestTaskDto(
                task.Id,
                workflow.Item2?.FindState(task.StateId).Name ?? "-",
                task.Status.ToString(),
                task.AssignedUserId,
                task.AssignedDepartmentId,
                task.CreatedAt,
                _jalali.ToJalali(task.CreatedAt)))
            .ToArray() ?? [];

        return Result.Success(new ServiceRequestDetailDto(
            order.Id,
            order.OrderNumber,
            order.CustomerId,
            customer?.DisplayName,
            customer?.Mobile,
            item?.ServiceId,
            item?.ItemName,
            item?.Quantity ?? 0,
            item?.UnitPriceSnapshot ?? 0,
            item?.AddonsTotal ?? 0,
            item?.EffectivePrice ?? 0,
            item?.AttributesJson,
            order.Status.ToString(),
            order.OrderDate,
            _jalali.ToJalali(order.OrderDate),
            workflow.Item1?.Id,
            workflow.Item2?.Code,
            ServiceRequestQuerySupport.StateCode(workflow.Item1, workflow.Item2),
            ServiceRequestQuerySupport.StateName(workflow.Item1, workflow.Item2),
            workflow.Item1?.Status.ToString(),
            tasks));
    }
}

public sealed class GetCustomerServiceRequestsQueryHandler
    : IRequestHandler<GetCustomerServiceRequestsQuery, Result<IReadOnlyList<ServiceRequestSummaryDto>>>
{
    private readonly IOrderReadRepository _readRepository;
    private readonly IOrderRepository _orders;
    private readonly IWorkflowInstanceRepository _instances;
    private readonly IWorkflowDefinitionRepository _definitions;
    private readonly IJalaliDateService _jalali;

    public GetCustomerServiceRequestsQueryHandler(
        IOrderReadRepository readRepository,
        IOrderRepository orders,
        IWorkflowInstanceRepository instances,
        IWorkflowDefinitionRepository definitions,
        IJalaliDateService jalali)
    {
        _readRepository = readRepository;
        _orders = orders;
        _instances = instances;
        _definitions = definitions;
        _jalali = jalali;
    }

    public async Task<Result<IReadOnlyList<ServiceRequestSummaryDto>>> Handle(
        GetCustomerServiceRequestsQuery request,
        CancellationToken cancellationToken)
    {
        var orders = await _readRepository.GetCustomerOrdersAsync(
            request.CustomerId,
            cancellationToken);
        var result = new List<ServiceRequestSummaryDto>();

        foreach (var summary in orders.Take(Math.Max(1, request.Limit)))
        {
            var order = await _orders.GetByIdWithDetailsAsync(summary.Id, cancellationToken);
            if (order is null)
            {
                continue;
            }

            var workflow = await ServiceRequestQuerySupport.FindWorkflowAsync(
                order.Id,
                _instances,
                _definitions,
                cancellationToken);
            result.Add(new ServiceRequestSummaryDto(
                order.Id,
                order.OrderNumber,
                order.CustomerId,
                summary.CustomerName,
                order.Items.FirstOrDefault(item => item.ServiceId.HasValue)?.ItemName,
                order.TotalAmount,
                order.Status.ToString(),
                order.OrderDate,
                _jalali.ToJalali(order.OrderDate),
                workflow.Item1?.Id,
                workflow.Item2?.Code,
                ServiceRequestQuerySupport.StateCode(workflow.Item1, workflow.Item2),
                ServiceRequestQuerySupport.StateName(workflow.Item1, workflow.Item2),
                workflow.Item1?.Status.ToString(),
                order.SalesExpertId));
        }

        return Result.Success<IReadOnlyList<ServiceRequestSummaryDto>>(result);
    }
}

public sealed class GetPortalServiceRequestsQueryHandler
    : IRequestHandler<GetPortalServiceRequestsQuery, Result<IReadOnlyList<ServiceRequestSummaryDto>>>
{
    private readonly ICustomerRepository _customers;
    private readonly IMediator _mediator;
    private readonly FoundationCurrentUser _currentUser;

    public GetPortalServiceRequestsQueryHandler(
        ICustomerRepository customers,
        IMediator mediator,
        FoundationCurrentUser currentUser)
    {
        _customers = customers;
        _mediator = mediator;
        _currentUser = currentUser;
    }

    public async Task<Result<IReadOnlyList<ServiceRequestSummaryDto>>> Handle(
        GetPortalServiceRequestsQuery request,
        CancellationToken cancellationToken)
    {
        if (_currentUser.UserId is null)
        {
            return Result.Failure<IReadOnlyList<ServiceRequestSummaryDto>>(
                "کاربر جاری قابل تشخیص نیست.",
                "PORTAL_USER_REQUIRED");
        }

        var customer = await _customers.GetByUserIdAsync(
            _currentUser.UserId.Value,
            false,
            cancellationToken);
        if (customer is null)
        {
            return Result.Failure<IReadOnlyList<ServiceRequestSummaryDto>>(
                "پروفایل مشتری یافت نشد.",
                "PORTAL_CUSTOMER_NOT_FOUND");
        }

        return await _mediator.Send(
            new GetCustomerServiceRequestsQuery(customer.Id),
            cancellationToken);
    }
}

public sealed class GetPortalCustomerServicesQueryHandler
    : IRequestHandler<GetPortalCustomerServicesQuery, Result<IReadOnlyList<CustomerServiceDto>>>
{
    private readonly ICustomerRepository _customers;
    private readonly ICustomerServiceRepository _services;
    private readonly FoundationCurrentUser _currentUser;
    private readonly IJalaliDateService _jalali;

    public GetPortalCustomerServicesQueryHandler(
        ICustomerRepository customers,
        ICustomerServiceRepository services,
        FoundationCurrentUser currentUser,
        IJalaliDateService jalali)
    {
        _customers = customers;
        _services = services;
        _currentUser = currentUser;
        _jalali = jalali;
    }

    public async Task<Result<IReadOnlyList<CustomerServiceDto>>> Handle(
        GetPortalCustomerServicesQuery request,
        CancellationToken cancellationToken)
    {
        if (_currentUser.UserId is null)
        {
            return Result.Failure<IReadOnlyList<CustomerServiceDto>>(
                "کاربر جاری قابل تشخیص نیست.",
                "PORTAL_USER_REQUIRED");
        }

        var customer = await _customers.GetByUserIdAsync(
            _currentUser.UserId.Value,
            false,
            cancellationToken);
        if (customer is null)
        {
            return Result.Failure<IReadOnlyList<CustomerServiceDto>>(
                "پروفایل مشتری یافت نشد.",
                "PORTAL_CUSTOMER_NOT_FOUND");
        }

        var services = await _services.GetByCustomerAsync(customer.Id, cancellationToken);
        var result = services.Select(service => new CustomerServiceDto(
            service.Id,
            service.CustomerId,
            service.ServiceId,
            service.OrderId,
            service.ServiceName,
            service.MonthlyAmount,
            service.Status.ToString(),
            service.ActivatedAt,
            _jalali.ToJalali(service.ActivatedAt),
            service.NextInvoiceAt,
            service.NextInvoiceAt.HasValue ? _jalali.ToJalali(service.NextInvoiceAt.Value) : null,
            service.CommissionBase)).ToArray();

        return Result.Success<IReadOnlyList<CustomerServiceDto>>(result);
    }
}

public sealed class GetRecentServiceRequestsQueryHandler
    : IRequestHandler<GetRecentServiceRequestsQuery, Result<IReadOnlyList<ServiceRequestSummaryDto>>>
{
    private readonly IOrderReadRepository _readRepository;
    private readonly IOrderRepository _orders;
    private readonly IWorkflowInstanceRepository _instances;
    private readonly IWorkflowDefinitionRepository _definitions;
    private readonly IJalaliDateService _jalali;

    public GetRecentServiceRequestsQueryHandler(
        IOrderReadRepository readRepository,
        IOrderRepository orders,
        IWorkflowInstanceRepository instances,
        IWorkflowDefinitionRepository definitions,
        IJalaliDateService jalali)
    {
        _readRepository = readRepository;
        _orders = orders;
        _instances = instances;
        _definitions = definitions;
        _jalali = jalali;
    }

    public async Task<Result<IReadOnlyList<ServiceRequestSummaryDto>>> Handle(
        GetRecentServiceRequestsQuery request,
        CancellationToken cancellationToken)
    {
        var page = await _readRepository.SearchAsync(
            new OrderSearchFilter(
                null,
                null,
                request.SalesExpertId,
                null,
                null,
                null,
                1,
                Math.Clamp(request.Limit, 1, 50)),
            cancellationToken);

        var result = new List<ServiceRequestSummaryDto>();
        foreach (var summary in page.Items)
        {
            var order = await _orders.GetByIdWithDetailsAsync(summary.Id, cancellationToken);
            if (order is null)
            {
                continue;
            }

            var workflow = await ServiceRequestQuerySupport.FindWorkflowAsync(
                order.Id,
                _instances,
                _definitions,
                cancellationToken);
            result.Add(new ServiceRequestSummaryDto(
                order.Id,
                order.OrderNumber,
                order.CustomerId,
                summary.CustomerName,
                order.Items.FirstOrDefault(item => item.ServiceId.HasValue)?.ItemName,
                order.TotalAmount,
                order.Status.ToString(),
                order.OrderDate,
                _jalali.ToJalali(order.OrderDate),
                workflow.Item1?.Id,
                workflow.Item2?.Code,
                ServiceRequestQuerySupport.StateCode(workflow.Item1, workflow.Item2),
                ServiceRequestQuerySupport.StateName(workflow.Item1, workflow.Item2),
                workflow.Item1?.Status.ToString(),
                order.SalesExpertId));
        }

        return Result.Success<IReadOnlyList<ServiceRequestSummaryDto>>(result);
    }
}
