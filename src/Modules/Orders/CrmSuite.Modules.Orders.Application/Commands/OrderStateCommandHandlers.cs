using CrmSuite.BuildingBlocks.Application.Abstractions;
using CrmSuite.BuildingBlocks.Domain.Exceptions;
using CrmSuite.BuildingBlocks.Domain.Results;
using CrmSuite.Modules.Catalog.Application.Abstractions;
using CrmSuite.Modules.Customers.Application.Abstractions;
using CrmSuite.Modules.Forms.Application.Abstractions;
using CrmSuite.Modules.Forms.Domain.Enums;
using CrmSuite.Modules.Foundation.Application.Abstractions;
using CrmSuite.Modules.Orders.Application.Abstractions;
using CrmSuite.Modules.Orders.Domain.Enums;
using CrmSuite.Modules.Orders.Application.Dtos;
using CrmSuite.Modules.Orders.Application.Mapping;
using MediatR;
using FoundationCurrentUser = CrmSuite.Modules.Foundation.Application.Abstractions.ICurrentUserService;

namespace CrmSuite.Modules.Orders.Application.Commands;

public sealed class UpdateOrderStatusCommandHandler
    : IRequestHandler<UpdateOrderStatusCommand, Result<OrderDto>>
{
    private readonly IOrderRepository _orders;
    private readonly IOrderUnitOfWork _unitOfWork;
    private readonly ICustomerRepository _customers;
    private readonly IAuditScope _auditScope;
    private readonly FoundationCurrentUser _currentUser;
    private readonly IDateTimeProvider _clock;
    private readonly OrderDtoMapper _mapper;
    private readonly IServiceFormLinkRepository? _formLinks;
    private readonly IFormSubmissionRepository? _formSubmissions;

    public UpdateOrderStatusCommandHandler(
        IOrderRepository orders,
        IOrderUnitOfWork unitOfWork,
        ICustomerRepository customers,
        IAuditScope auditScope,
        FoundationCurrentUser currentUser,
        IDateTimeProvider clock,
        OrderDtoMapper mapper,
        IServiceFormLinkRepository? formLinks = null,
        IFormSubmissionRepository? formSubmissions = null)
    {
        _orders = orders;
        _unitOfWork = unitOfWork;
        _customers = customers;
        _auditScope = auditScope;
        _currentUser = currentUser;
        _clock = clock;
        _mapper = mapper;
        _formLinks = formLinks;
        _formSubmissions = formSubmissions;
    }

    public async Task<Result<OrderDto>> Handle(
        UpdateOrderStatusCommand request,
        CancellationToken cancellationToken)
    {
        var order = await _orders.GetByIdWithDetailsAsync(request.Id, cancellationToken);
        if (order is null)
        {
            return Result.Failure<OrderDto>("سفارش یافت نشد.", "ORDERS_NOT_FOUND");
        }

        var customer = await _customers.GetByIdAsync(order.CustomerId, false, cancellationToken);

        if (request.Status == OrderStatus.PendingPayment
            && _formLinks is not null
            && _formSubmissions is not null)
        {
            var serviceIds = order.Items
                .Where(item => item.ServiceId.HasValue)
                .Select(item => item.ServiceId!.Value)
                .Distinct();

            foreach (var serviceId in serviceIds)
            {
                var links = await _formLinks.GetByServiceAsync(serviceId, cancellationToken);
                foreach (var link in links.Where(link => link.IsRequired))
                {
                    var submitted = await _formSubmissions.ExistsForEntityAsync(
                        link.FormDefinitionId,
                        FormEntityType.Order,
                        order.Id,
                        cancellationToken);
                    if (!submitted)
                    {
                        return Result.Failure<OrderDto>(
                            "برای نهایی‌سازی سفارش، فرم‌های اجباری سرویس باید تکمیل شوند.",
                            "ORDERS_REQUIRED_FORMS_MISSING");
                    }
                }
            }
        }

        _auditScope.OldValues = _mapper.ToOrderDto(order, customer?.DisplayName);
        _auditScope.EntityId = order.Id.ToString();

        try
        {
            order.ChangeStatus(request.Status, _currentUser.UserId, _clock.UtcNow, request.Note);
        }
        catch (DomainException exception)
        {
            return Result.Failure<OrderDto>(exception.Message, exception.Code);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        var updated = await _orders.GetByIdWithDetailsAsync(order.Id, cancellationToken);

        return Result.Success(_mapper.ToOrderDto(updated ?? order, customer?.DisplayName));
    }
}

public sealed class CancelOrderCommandHandler : IRequestHandler<CancelOrderCommand, Result<OrderDto>>
{
    private readonly IOrderRepository _orders;
    private readonly IOrderUnitOfWork _unitOfWork;
    private readonly ICustomerRepository _customers;
    private readonly IAuditScope _auditScope;
    private readonly FoundationCurrentUser _currentUser;
    private readonly IDateTimeProvider _clock;
    private readonly OrderDtoMapper _mapper;

    public CancelOrderCommandHandler(
        IOrderRepository orders,
        IOrderUnitOfWork unitOfWork,
        ICustomerRepository customers,
        IAuditScope auditScope,
        FoundationCurrentUser currentUser,
        IDateTimeProvider clock,
        OrderDtoMapper mapper)
    {
        _orders = orders;
        _unitOfWork = unitOfWork;
        _customers = customers;
        _auditScope = auditScope;
        _currentUser = currentUser;
        _clock = clock;
        _mapper = mapper;
    }

    public async Task<Result<OrderDto>> Handle(
        CancelOrderCommand request,
        CancellationToken cancellationToken)
    {
        var order = await _orders.GetByIdWithDetailsAsync(request.Id, cancellationToken);
        if (order is null)
        {
            return Result.Failure<OrderDto>("سفارش یافت نشد.", "ORDERS_NOT_FOUND");
        }

        var customer = await _customers.GetByIdAsync(order.CustomerId, false, cancellationToken);
        _auditScope.OldValues = _mapper.ToOrderDto(order, customer?.DisplayName);
        _auditScope.EntityId = order.Id.ToString();

        try
        {
            order.Cancel(_currentUser.UserId, _clock.UtcNow, request.Note);
        }
        catch (DomainException exception)
        {
            return Result.Failure<OrderDto>(exception.Message, exception.Code);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        var updated = await _orders.GetByIdWithDetailsAsync(order.Id, cancellationToken);

        return Result.Success(_mapper.ToOrderDto(updated ?? order, customer?.DisplayName));
    }
}

public sealed class ApplyDiscountCommandHandler
    : IRequestHandler<ApplyDiscountCommand, Result<OrderDto>>
{
    private readonly IOrderRepository _orders;
    private readonly IOrderUnitOfWork _unitOfWork;
    private readonly ICustomerRepository _customers;
    private readonly IAuditScope _auditScope;
    private readonly FoundationCurrentUser _currentUser;
    private readonly IDateTimeProvider _clock;
    private readonly OrderDtoMapper _mapper;

    public ApplyDiscountCommandHandler(
        IOrderRepository orders,
        IOrderUnitOfWork unitOfWork,
        ICustomerRepository customers,
        IAuditScope auditScope,
        FoundationCurrentUser currentUser,
        IDateTimeProvider clock,
        OrderDtoMapper mapper)
    {
        _orders = orders;
        _unitOfWork = unitOfWork;
        _customers = customers;
        _auditScope = auditScope;
        _currentUser = currentUser;
        _clock = clock;
        _mapper = mapper;
    }

    public async Task<Result<OrderDto>> Handle(
        ApplyDiscountCommand request,
        CancellationToken cancellationToken)
    {
        var order = await _orders.GetByIdWithDetailsAsync(request.Id, cancellationToken);
        if (order is null)
        {
            return Result.Failure<OrderDto>("سفارش یافت نشد.", "ORDERS_NOT_FOUND");
        }

        var customer = await _customers.GetByIdAsync(order.CustomerId, false, cancellationToken);
        _auditScope.OldValues = _mapper.ToOrderDto(order, customer?.DisplayName);
        _auditScope.EntityId = order.Id.ToString();

        try
        {
            order.ApplyDiscount(request.DiscountAmount, _currentUser.UserId, _clock.UtcNow);
        }
        catch (DomainException exception)
        {
            return Result.Failure<OrderDto>(exception.Message, exception.Code);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        var updated = await _orders.GetByIdWithDetailsAsync(order.Id, cancellationToken);

        return Result.Success(_mapper.ToOrderDto(updated ?? order, customer?.DisplayName));
    }
}
