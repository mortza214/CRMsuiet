using CrmSuite.BuildingBlocks.Application.Security;
using CrmSuite.Modules.Foundation.Api.Extensions;
using CrmSuite.Modules.Workflow.Application.Commands;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CrmSuite.Modules.Workflow.Api.Controllers;

[ApiController]
[Route("api/portal/workflow")]
[Authorize(Roles = Roles.Customer)]
public sealed class PortalWorkflowController : ControllerBase
{
    private readonly IMediator _mediator;

    public PortalWorkflowController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost("instances/{instanceId:guid}/action")]
    public async Task<IActionResult> ExecuteAction(
        Guid instanceId,
        [FromBody] PortalWorkflowActionRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            new ExecuteWorkflowActionCommand(
                instanceId,
                request.TransitionId,
                request.ActionDataJson),
            cancellationToken);
        return this.ToApiResult(result, "اقدام با موفقیت انجام شد.");
    }
}

public sealed class PortalWorkflowActionRequest
{
    public Guid? TransitionId { get; init; }

    public string? ActionDataJson { get; init; }
}
