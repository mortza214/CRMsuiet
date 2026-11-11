using CrmSuite.BuildingBlocks.Application.Security;
using CrmSuite.Modules.Foundation.Api.Extensions;
using CrmSuite.Modules.Identity.Api.Authorization;
using CrmSuite.Modules.Notifications.Application.Commands;
using CrmSuite.Modules.Notifications.Application.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CrmSuite.Modules.Notifications.Api.Controllers;

[ApiController]
[Route("api/notifications")]
[Authorize]
[HasPermission(Permissions.Notifications.Notification.Read)]
public sealed class NotificationsController : ControllerBase
{
    private readonly IMediator _mediator;

    public NotificationsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet("my")]
    public async Task<IActionResult> GetMyNotifications(CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetMyNotificationsQuery(), cancellationToken);
        return this.ToApiResult(result);
    }

    [HttpPut("{id:guid}/read")]
    public async Task<IActionResult> MarkRead(Guid id, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new MarkNotificationReadCommand(id), cancellationToken);
        return this.ToApiResult(result, "اعلان خوانده شد.");
    }
}
