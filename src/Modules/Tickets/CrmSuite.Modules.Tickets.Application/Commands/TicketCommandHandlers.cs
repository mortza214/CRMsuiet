using CrmSuite.BuildingBlocks.Application.Abstractions;
using CrmSuite.BuildingBlocks.Domain.Results;
using CrmSuite.Modules.Catalog.Application.Abstractions;
using CrmSuite.Modules.Customers.Application.Abstractions;
using CrmSuite.Modules.Customers.Domain.Entities;
using CrmSuite.Modules.Foundation.Application.Abstractions;
using CrmSuite.Modules.Identity.Application.Abstractions;
using CrmSuite.Modules.Tickets.Application.Abstractions;
using CrmSuite.Modules.Tickets.Application.Dtos;
using CrmSuite.Modules.Tickets.Application.Notifications;
using CrmSuite.Modules.Tickets.Application.Services;
using CrmSuite.Modules.Tickets.Domain.Entities;
using CrmSuite.Modules.Tickets.Domain.Enums;
using MediatR;
using FoundationCurrentUser = CrmSuite.Modules.Foundation.Application.Abstractions.ICurrentUserService;

namespace CrmSuite.Modules.Tickets.Application.Commands;

public sealed class CreateTicketCommandHandler
    : IRequestHandler<CreateTicketCommand, Result<TicketDto>>
{
    private readonly ITicketRepository _tickets;
    private readonly ITicketUnitOfWork _unitOfWork;
    private readonly ICustomerRepository _customers;
    private readonly IServiceRepository _services;
    private readonly IDepartmentRepository _departments;
    private readonly StaffAccessService _staffAccess;
    private readonly INotificationService _notifications;
    private readonly ISequenceService _sequences;
    private readonly IMediator _mediator;
    private readonly IAuditScope _auditScope;
    private readonly FoundationCurrentUser _currentUser;
    private readonly IDateTimeProvider _clock;
    private readonly TicketViewService _view;

    public CreateTicketCommandHandler(
        ITicketRepository tickets,
        ITicketUnitOfWork unitOfWork,
        ICustomerRepository customers,
        IServiceRepository services,
        IDepartmentRepository departments,
        StaffAccessService staffAccess,
        INotificationService notifications,
        ISequenceService sequences,
        IMediator mediator,
        IAuditScope auditScope,
        FoundationCurrentUser currentUser,
        IDateTimeProvider clock,
        TicketViewService view)
    {
        _tickets = tickets;
        _unitOfWork = unitOfWork;
        _customers = customers;
        _services = services;
        _departments = departments;
        _staffAccess = staffAccess;
        _notifications = notifications;
        _sequences = sequences;
        _mediator = mediator;
        _auditScope = auditScope;
        _currentUser = currentUser;
        _clock = clock;
        _view = view;
    }

    public async Task<Result<TicketDto>> Handle(
        CreateTicketCommand request,
        CancellationToken cancellationToken)
    {
        if (_currentUser.UserId is null)
        {
            return Result.Failure<TicketDto>("کاربر جاری قابل تشخیص نیست.", "TICKETS_CURRENT_USER_MISSING");
        }

        Customer? customer;
        if (request.Source == TicketSource.Portal)
        {
            customer = await _customers.GetByUserIdAsync(
                _currentUser.UserId.Value,
                false,
                cancellationToken);

            if (customer is null)
            {
                return Result.Failure<TicketDto>("پروفایل مشتری یافت نشد.", "TICKETS_CUSTOMER_NOT_FOUND");
            }
        }
        else
        {
            if (!request.CustomerId.HasValue)
            {
                return Result.Failure<TicketDto>("انتخاب مشتری الزامی است.", "TICKETS_CUSTOMER_REQUIRED");
            }

            customer = await _customers.GetByIdAsync(
                request.CustomerId.Value,
                false,
                cancellationToken);

            if (customer is null)
            {
                return Result.Failure<TicketDto>("مشتری یافت نشد.", "TICKETS_CUSTOMER_NOT_FOUND");
            }

            var staff = await _staffAccess.GetAsync(cancellationToken);
            if (staff is null)
            {
                return Result.Failure<TicketDto>("دسترسی کارمند قابل تشخیص نیست.", "TICKETS_STAFF_ACCESS_MISSING");
            }

            if (!staff.CanAccessDepartment(request.DepartmentId))
            {
                return Result.Failure<TicketDto>(
                    "دپارتمان انتخاب‌شده در دسترسی شما نیست.",
                    "TICKETS_DEPARTMENT_FORBIDDEN");
            }
        }

        var department = await _departments.GetByIdAsync(request.DepartmentId, cancellationToken);
        if (department is null)
        {
            return Result.Failure<TicketDto>("دپارتمان یافت نشد.", "TICKETS_DEPARTMENT_NOT_FOUND");
        }

        if (request.ServiceId.HasValue)
        {
            var service = await _services.GetByIdAsync(request.ServiceId.Value, cancellationToken);
            if (service is null)
            {
                return Result.Failure<TicketDto>("سرویس یافت نشد.", "TICKETS_SERVICE_NOT_FOUND");
            }
        }

        if (request.AssignedToUserId.HasValue
            && !await _staffAccess.IsInDepartmentAsync(
                request.AssignedToUserId.Value,
                request.DepartmentId,
                cancellationToken))
        {
            return Result.Failure<TicketDto>(
                "کارشناس انتخاب‌شده در دپارتمان تیکت نیست.",
                "TICKETS_ASSIGNEE_INVALID");
        }

        var now = _clock.UtcNow;
        var ticketNumber = await _sequences.GetNextValueAsync("TicketNumber", cancellationToken);

        var ticket = new Ticket(
            ticketNumber,
            customer.Id,
            request.ServiceId,
            request.Subject,
            request.DepartmentId,
            request.AssignedToUserId,
            request.Priority,
            request.Source,
            _currentUser.UserId,
            now);

        ticket.AddInitialHistory(_currentUser.UserId, now);

        var senderType = request.Source == TicketSource.Portal
            ? TicketSenderType.Customer
            : TicketSenderType.Employee;

        ticket.AddMessage(new TicketMessage(
            ticket.Id,
            _currentUser.UserId.Value,
            senderType,
            request.Message,
            false,
            now));

        _tickets.Add(ticket);

        await _notifications.NotifyDepartmentAsync(
            ticket.DepartmentId,
            $"تیکت جدید {ticket.TicketNumber}",
            ticket.Subject,
            cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        _auditScope.EntityId = ticket.Id.ToString();

        try
        {
            await _mediator.Publish(
                new TicketCreatedNotification(
                    ticket.Id,
                    ticket.TicketNumber,
                    ticket.CustomerId,
                    ticket.DepartmentId,
                    ticket.Subject),
                cancellationToken);
        }
        catch
        {
            // Notification delivery must not roll back a saved ticket.
        }

        return Result.Success(await _view.BuildAsync(ticket, true, cancellationToken));
    }
}

public sealed class AddTicketMessageCommandHandler
    : IRequestHandler<AddTicketMessageCommand, Result<TicketDto>>
{
    private readonly ITicketRepository _tickets;
    private readonly ITicketUnitOfWork _unitOfWork;
    private readonly ICustomerRepository _customers;
    private readonly StaffAccessService _staffAccess;
    private readonly INotificationService _notifications;
    private readonly IAuditScope _auditScope;
    private readonly FoundationCurrentUser _currentUser;
    private readonly IDateTimeProvider _clock;
    private readonly TicketViewService _view;

    public AddTicketMessageCommandHandler(
        ITicketRepository tickets,
        ITicketUnitOfWork unitOfWork,
        ICustomerRepository customers,
        StaffAccessService staffAccess,
        INotificationService notifications,
        IAuditScope auditScope,
        FoundationCurrentUser currentUser,
        IDateTimeProvider clock,
        TicketViewService view)
    {
        _tickets = tickets;
        _unitOfWork = unitOfWork;
        _customers = customers;
        _staffAccess = staffAccess;
        _notifications = notifications;
        _auditScope = auditScope;
        _currentUser = currentUser;
        _clock = clock;
        _view = view;
    }

    public async Task<Result<TicketDto>> Handle(
        AddTicketMessageCommand request,
        CancellationToken cancellationToken)
    {
        var ticket = await _tickets.GetByIdWithDetailsAsync(request.TicketId, cancellationToken);
        if (ticket is null)
        {
            return Result.Failure<TicketDto>("تیکت یافت نشد.", "TICKETS_NOT_FOUND");
        }

        if (_currentUser.UserId is null)
        {
            return Result.Failure<TicketDto>("کاربر جاری قابل تشخیص نیست.", "TICKETS_CURRENT_USER_MISSING");
        }

        var isPortal = false;

        if (request.SenderType == TicketSenderType.Customer)
        {
            var customer = await _customers.GetByUserIdAsync(
                _currentUser.UserId.Value,
                false,
                cancellationToken);

            if (customer is null || customer.Id != ticket.CustomerId)
            {
                return Result.Failure<TicketDto>("تیکت یافت نشد.", "TICKETS_NOT_FOUND");
            }

            if (request.IsInternalNote)
            {
                return Result.Failure<TicketDto>(
                    "مشتری نمی‌تواند یادداشت داخلی ثبت کند.",
                    "TICKETS_INTERNAL_NOTE_FORBIDDEN");
            }

            isPortal = true;
        }
        else
        {
            var staff = await _staffAccess.GetAsync(cancellationToken);
            if (staff is null || !staff.CanAccessDepartment(ticket.DepartmentId))
            {
                return Result.Failure<TicketDto>("دسترسی به این تیکت وجود ندارد.", "TICKETS_FORBIDDEN");
            }
        }

        ticket.AddMessage(new TicketMessage(
            ticket.Id,
            _currentUser.UserId.Value,
            request.SenderType,
            request.Message,
            request.IsInternalNote,
            _clock.UtcNow));

        if (isPortal)
        {
            await _notifications.NotifyDepartmentAsync(
                ticket.DepartmentId,
                $"پاسخ جدید مشتری در {ticket.TicketNumber}",
                request.Message,
                cancellationToken);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        _auditScope.EntityId = ticket.Id.ToString();

        return Result.Success(await _view.BuildAsync(ticket, !isPortal, cancellationToken));
    }
}

public sealed class ChangeTicketStatusCommandHandler
    : IRequestHandler<ChangeTicketStatusCommand, Result<TicketDto>>
{
    private readonly ITicketRepository _tickets;
    private readonly ITicketUnitOfWork _unitOfWork;
    private readonly StaffAccessService _staffAccess;
    private readonly IAuditScope _auditScope;
    private readonly FoundationCurrentUser _currentUser;
    private readonly IDateTimeProvider _clock;
    private readonly TicketViewService _view;

    public ChangeTicketStatusCommandHandler(
        ITicketRepository tickets,
        ITicketUnitOfWork unitOfWork,
        StaffAccessService staffAccess,
        IAuditScope auditScope,
        FoundationCurrentUser currentUser,
        IDateTimeProvider clock,
        TicketViewService view)
    {
        _tickets = tickets;
        _unitOfWork = unitOfWork;
        _staffAccess = staffAccess;
        _auditScope = auditScope;
        _currentUser = currentUser;
        _clock = clock;
        _view = view;
    }

    public async Task<Result<TicketDto>> Handle(
        ChangeTicketStatusCommand request,
        CancellationToken cancellationToken)
    {
        var ticket = await _tickets.GetByIdWithDetailsAsync(request.TicketId, cancellationToken);
        if (ticket is null)
        {
            return Result.Failure<TicketDto>("تیکت یافت نشد.", "TICKETS_NOT_FOUND");
        }

        var staff = await _staffAccess.GetAsync(cancellationToken);
        if (staff is null || !staff.CanAccessDepartment(ticket.DepartmentId))
        {
            return Result.Failure<TicketDto>("دسترسی به این تیکت وجود ندارد.", "TICKETS_FORBIDDEN");
        }

        try
        {
            ticket.ChangeStatus(request.Status, _currentUser.UserId, _clock.UtcNow, request.Note);
        }
        catch (CrmSuite.BuildingBlocks.Domain.Exceptions.DomainException exception)
        {
            return Result.Failure<TicketDto>(exception.Message, exception.Code);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        _auditScope.OldValues = null;
        _auditScope.EntityId = ticket.Id.ToString();

        return Result.Success(await _view.BuildAsync(ticket, true, cancellationToken));
    }
}

public sealed class AssignTicketCommandHandler
    : IRequestHandler<AssignTicketCommand, Result<TicketDto>>
{
    private readonly ITicketRepository _tickets;
    private readonly ITicketUnitOfWork _unitOfWork;
    private readonly StaffAccessService _staffAccess;
    private readonly IAuditScope _auditScope;
    private readonly FoundationCurrentUser _currentUser;
    private readonly IDateTimeProvider _clock;
    private readonly TicketViewService _view;

    public AssignTicketCommandHandler(
        ITicketRepository tickets,
        ITicketUnitOfWork unitOfWork,
        StaffAccessService staffAccess,
        IAuditScope auditScope,
        FoundationCurrentUser currentUser,
        IDateTimeProvider clock,
        TicketViewService view)
    {
        _tickets = tickets;
        _unitOfWork = unitOfWork;
        _staffAccess = staffAccess;
        _auditScope = auditScope;
        _currentUser = currentUser;
        _clock = clock;
        _view = view;
    }

    public async Task<Result<TicketDto>> Handle(
        AssignTicketCommand request,
        CancellationToken cancellationToken)
    {
        var ticket = await _tickets.GetByIdWithDetailsAsync(request.TicketId, cancellationToken);
        if (ticket is null)
        {
            return Result.Failure<TicketDto>("تیکت یافت نشد.", "TICKETS_NOT_FOUND");
        }

        var staff = await _staffAccess.GetAsync(cancellationToken);
        if (staff is null || !staff.CanAccessDepartment(ticket.DepartmentId))
        {
            return Result.Failure<TicketDto>("دسترسی به این تیکت وجود ندارد.", "TICKETS_FORBIDDEN");
        }

        if (request.AssignedToUserId.HasValue
            && !await _staffAccess.IsInDepartmentAsync(
                request.AssignedToUserId.Value,
                ticket.DepartmentId,
                cancellationToken))
        {
            return Result.Failure<TicketDto>(
                "کارشناس انتخاب‌شده در دپارتمان تیکت نیست.",
                "TICKETS_ASSIGNEE_INVALID");
        }

        ticket.Assign(request.AssignedToUserId, _clock.UtcNow);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        _auditScope.EntityId = ticket.Id.ToString();

        return Result.Success(await _view.BuildAsync(ticket, true, cancellationToken));
    }
}
