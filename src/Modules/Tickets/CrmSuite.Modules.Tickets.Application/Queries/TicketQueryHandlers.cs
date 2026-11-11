using CrmSuite.BuildingBlocks.Domain.Results;
using CrmSuite.Modules.Customers.Application.Abstractions;
using CrmSuite.Modules.Tickets.Application.Abstractions;
using CrmSuite.Modules.Tickets.Application.Dtos;
using CrmSuite.Modules.Tickets.Application.Services;
using MediatR;
using FoundationCurrentUser = CrmSuite.Modules.Foundation.Application.Abstractions.ICurrentUserService;

namespace CrmSuite.Modules.Tickets.Application.Queries;

public sealed class GetTicketsQueryHandler
    : IRequestHandler<GetTicketsQuery, Result<PagedResult<TicketListItemDto>>>
{
    private readonly ITicketReadRepository _readRepository;
    private readonly StaffAccessService _staffAccess;

    public GetTicketsQueryHandler(
        ITicketReadRepository readRepository,
        StaffAccessService staffAccess)
    {
        _readRepository = readRepository;
        _staffAccess = staffAccess;
    }

    public async Task<Result<PagedResult<TicketListItemDto>>> Handle(
        GetTicketsQuery request,
        CancellationToken cancellationToken)
    {
        var staff = await _staffAccess.GetAsync(cancellationToken);
        if (staff is null)
        {
            return Result.Failure<PagedResult<TicketListItemDto>>(
                "دسترسی کارمند قابل تشخیص نیست.",
                "TICKETS_STAFF_ACCESS_MISSING");
        }

        if (!staff.IsAdmin && staff.DepartmentIds.Count == 0)
        {
            return Result.Success(new PagedResult<TicketListItemDto>(
                [],
                0,
                request.Page,
                request.PageSize));
        }

        var page = await _readRepository.SearchAsync(
            new TicketSearchFilter(
                request.Status,
                request.Priority,
                request.DepartmentId,
                request.CustomerId,
                request.Search,
                staff.IsAdmin ? null : staff.DepartmentIds,
                request.Page,
                request.PageSize),
            cancellationToken);

        return Result.Success(page);
    }
}

public sealed class GetTicketByIdQueryHandler
    : IRequestHandler<GetTicketByIdQuery, Result<TicketDto>>
{
    private readonly ITicketRepository _tickets;
    private readonly ICustomerRepository _customers;
    private readonly StaffAccessService _staffAccess;
    private readonly FoundationCurrentUser _currentUser;
    private readonly TicketViewService _view;

    public GetTicketByIdQueryHandler(
        ITicketRepository tickets,
        ICustomerRepository customers,
        StaffAccessService staffAccess,
        FoundationCurrentUser currentUser,
        TicketViewService view)
    {
        _tickets = tickets;
        _customers = customers;
        _staffAccess = staffAccess;
        _currentUser = currentUser;
        _view = view;
    }

    public async Task<Result<TicketDto>> Handle(
        GetTicketByIdQuery request,
        CancellationToken cancellationToken)
    {
        var ticket = await _tickets.GetByIdWithDetailsAsync(request.Id, cancellationToken);
        if (ticket is null)
        {
            return Result.Failure<TicketDto>("تیکت یافت نشد.", "TICKETS_NOT_FOUND");
        }

        if (request.Portal)
        {
            if (_currentUser.UserId is null)
            {
                return Result.Failure<TicketDto>("کاربر جاری قابل تشخیص نیست.", "TICKETS_CURRENT_USER_MISSING");
            }

            var customer = await _customers.GetByUserIdAsync(
                _currentUser.UserId.Value,
                false,
                cancellationToken);

            if (customer is null || customer.Id != ticket.CustomerId)
            {
                return Result.Failure<TicketDto>("تیکت یافت نشد.", "TICKETS_NOT_FOUND");
            }

            return Result.Success(await _view.BuildAsync(ticket, false, cancellationToken));
        }

        var staff = await _staffAccess.GetAsync(cancellationToken);
        if (staff is null || !staff.CanAccessDepartment(ticket.DepartmentId))
        {
            return Result.Failure<TicketDto>("دسترسی به این تیکت وجود ندارد.", "TICKETS_FORBIDDEN");
        }

        return Result.Success(await _view.BuildAsync(ticket, true, cancellationToken));
    }

}

public sealed class GetMyTicketsQueryHandler
    : IRequestHandler<GetMyTicketsQuery, Result<IReadOnlyList<TicketListItemDto>>>
{
    private readonly ICustomerRepository _customers;
    private readonly ITicketReadRepository _readRepository;
    private readonly FoundationCurrentUser _currentUser;

    public GetMyTicketsQueryHandler(
        ICustomerRepository customers,
        ITicketReadRepository readRepository,
        FoundationCurrentUser currentUser)
    {
        _customers = customers;
        _readRepository = readRepository;
        _currentUser = currentUser;
    }

    public async Task<Result<IReadOnlyList<TicketListItemDto>>> Handle(
        GetMyTicketsQuery request,
        CancellationToken cancellationToken)
    {
        var customer = await _customers.GetByUserIdAsync(
            _currentUser.UserId ?? Guid.Empty,
            false,
            cancellationToken);

        if (customer is null)
        {
            return Result.Failure<IReadOnlyList<TicketListItemDto>>(
                "پروفایل مشتری یافت نشد.",
                "TICKETS_CUSTOMER_NOT_FOUND");
        }

        var tickets = await _readRepository.GetCustomerTicketsAsync(customer.Id, cancellationToken);
        return Result.Success(tickets);
    }
}
