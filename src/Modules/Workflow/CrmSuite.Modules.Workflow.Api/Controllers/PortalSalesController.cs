using CrmSuite.BuildingBlocks.Application.Security;
using CrmSuite.Modules.Foundation.Api.Extensions;
using CrmSuite.Modules.Workflow.Api.Contracts;
using CrmSuite.Modules.Workflow.Application.Sales;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CrmSuite.Modules.Workflow.Api.Controllers;

[ApiController]
[Route("api/portal/sales")]
[Authorize(Roles = Roles.Customer)]
public sealed class PortalSalesController : ControllerBase
{
    private readonly IMediator _mediator;

    public PortalSalesController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet("requests")]
    public async Task<IActionResult> GetMyRequests(CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            new GetPortalServiceRequestsQuery(),
            cancellationToken);
        return this.ToApiResult(result);
    }

    [HttpPost("instances/{instanceId:guid}/confirm-proforma")]
    public async Task<IActionResult> ConfirmProforma(
        Guid instanceId,
        [FromBody] ConfirmProformaRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            new ConfirmProformaCommand(instanceId, request.Approved, request.NeedsInstallation),
            cancellationToken);
        return this.ToApiResult(result, "پاسخ شما ثبت شد.");
    }

    [HttpGet("services")]
    public async Task<IActionResult> GetMyServices(CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            new GetPortalCustomerServicesQuery(),
            cancellationToken);
        return this.ToApiResult(result);
    }
}
