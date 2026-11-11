using CrmSuite.BuildingBlocks.Application.Security;
using CrmSuite.Modules.Foundation.Api.Extensions;
using CrmSuite.Modules.Foundation.Api.Filters;
using CrmSuite.Modules.Identity.Api.Authorization;
using CrmSuite.Modules.Tickets.Api.Contracts;
using CrmSuite.Modules.Tickets.Application.Commands;
using CrmSuite.Modules.Tickets.Application.Queries;
using CrmSuite.Modules.Tickets.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CrmSuite.Modules.Tickets.Api.Controllers;

[ApiController]
[Route("api/tickets")]
[Authorize]
[Audit]
public sealed class TicketsController : ControllerBase
{
    private readonly IMediator _mediator;

    public TicketsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    [HasPermission(Permissions.Tickets.Ticket.Read)]
    public async Task<IActionResult> GetTickets(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] TicketStatus? status = null,
        [FromQuery] TicketPriority? priority = null,
        [FromQuery] Guid? departmentId = null,
        [FromQuery] Guid? customerId = null,
        [FromQuery] string? search = null,
        CancellationToken cancellationToken = default)
    {
        var result = await _mediator.Send(
            new GetTicketsQuery(page, pageSize, status, priority, departmentId, customerId, search),
            cancellationToken);

        return this.ToApiResult(result);
    }

    [HttpGet("{id:guid}")]
    [HasPermission(Permissions.Tickets.Ticket.Read)]
    public async Task<IActionResult> GetTicket(Guid id, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetTicketByIdQuery(id, false), cancellationToken);
        return this.ToApiResult(result);
    }

    [HttpPost]
    [HasPermission(Permissions.Tickets.Ticket.Create)]
    public async Task<IActionResult> Create(
        [FromBody] CreateStaffTicketRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            new CreateTicketCommand(
                request.CustomerId,
                request.ServiceId,
                request.Subject,
                request.DepartmentId,
                request.Priority,
                request.Source,
                request.Message,
                request.AssignedToUserId),
            cancellationToken);

        return this.ToApiResult(result, "تیکت ایجاد شد.");
    }

    [HttpPost("{id:guid}/messages")]
    [HasPermission(Permissions.Tickets.Ticket.Reply)]
    public async Task<IActionResult> AddMessage(
        Guid id,
        [FromBody] TicketMessageRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            new AddTicketMessageCommand(
                id,
                request.Message,
                request.IsInternalNote,
                TicketSenderType.Employee),
            cancellationToken);

        return this.ToApiResult(result, "پیام ثبت شد.");
    }

    [HttpPut("{id:guid}/status")]
    [HasPermission(Permissions.Tickets.Ticket.Close)]
    public async Task<IActionResult> ChangeStatus(
        Guid id,
        [FromBody] ChangeTicketStatusRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            new ChangeTicketStatusCommand(id, request.Status, request.Note),
            cancellationToken);

        return this.ToApiResult(result, "وضعیت تیکت تغییر کرد.");
    }

    [HttpPut("{id:guid}/assign")]
    [HasPermission(Permissions.Tickets.Ticket.Assign)]
    public async Task<IActionResult> Assign(
        Guid id,
        [FromBody] AssignTicketRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            new AssignTicketCommand(id, request.AssignedToUserId),
            cancellationToken);

        return this.ToApiResult(result, "تیکت اختصاص یافت.");
    }
}
