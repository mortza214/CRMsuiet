using CrmSuite.BuildingBlocks.Application.Security;
using CrmSuite.Modules.Foundation.Api.Extensions;
using CrmSuite.Modules.Tickets.Api.Contracts;
using CrmSuite.Modules.Tickets.Application.Commands;
using CrmSuite.Modules.Tickets.Application.Queries;
using CrmSuite.Modules.Tickets.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CrmSuite.Modules.Tickets.Api.Controllers.Portal;

[ApiController]
[Route("api/portal/tickets")]
[Authorize(Roles = Roles.Customer)]
public sealed class TicketsController : ControllerBase
{
    private readonly IMediator _mediator;

    public TicketsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    public async Task<IActionResult> GetMyTickets(CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetMyTicketsQuery(), cancellationToken);
        return this.ToApiResult(result);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetTicket(Guid id, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetTicketByIdQuery(id, true), cancellationToken);
        return this.ToApiResult(result);
    }

    [HttpPost]
    public async Task<IActionResult> Create(
        [FromBody] CreatePortalTicketRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            new CreateTicketCommand(
                null,
                request.ServiceId,
                request.Subject,
                request.DepartmentId,
                request.Priority,
                TicketSource.Portal,
                request.Message),
            cancellationToken);

        return this.ToApiResult(result, "تیکت ثبت شد.");
    }

    [HttpPost("{id:guid}/messages")]
    public async Task<IActionResult> AddMessage(
        Guid id,
        [FromBody] TicketMessageRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            new AddTicketMessageCommand(
                id,
                request.Message,
                false,
                TicketSenderType.Customer),
            cancellationToken);

        return this.ToApiResult(result, "پیام ثبت شد.");
    }
}
