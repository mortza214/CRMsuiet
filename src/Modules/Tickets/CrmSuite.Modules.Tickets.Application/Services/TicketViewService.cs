using CrmSuite.Modules.Catalog.Application.Abstractions;
using CrmSuite.Modules.Customers.Application.Abstractions;
using CrmSuite.Modules.Identity.Application.Abstractions;
using CrmSuite.Modules.Tickets.Application.Dtos;
using CrmSuite.Modules.Tickets.Application.Mapping;
using CrmSuite.Modules.Tickets.Domain.Entities;

namespace CrmSuite.Modules.Tickets.Application.Services;

public sealed class TicketViewService
{
    private readonly ICustomerRepository _customers;
    private readonly IServiceRepository _services;
    private readonly IDepartmentRepository _departments;
    private readonly IUserRepository _users;
    private readonly TicketDtoMapper _mapper;

    public TicketViewService(
        ICustomerRepository customers,
        IServiceRepository services,
        IDepartmentRepository departments,
        IUserRepository users,
        TicketDtoMapper mapper)
    {
        _customers = customers;
        _services = services;
        _departments = departments;
        _users = users;
        _mapper = mapper;
    }

    public async Task<TicketDto> BuildAsync(
        Ticket ticket,
        bool includeInternalNotes,
        CancellationToken cancellationToken)
    {
        var customer = await _customers.GetByIdAsync(ticket.CustomerId, false, cancellationToken);
        var service = ticket.ServiceId.HasValue
            ? await _services.GetByIdAsync(ticket.ServiceId.Value, cancellationToken)
            : null;
        var department = await _departments.GetByIdAsync(ticket.DepartmentId, cancellationToken);
        var assignee = ticket.AssignedToUserId.HasValue
            ? await _users.GetByIdAsync(ticket.AssignedToUserId.Value, cancellationToken)
            : null;

        return _mapper.ToDto(
            ticket,
            customer?.DisplayName,
            service?.Name,
            department?.Name,
            assignee?.FullName,
            includeInternalNotes);
    }
}
