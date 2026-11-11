using CrmSuite.BuildingBlocks.Application.Security;
using CrmSuite.Modules.Foundation.Api.Extensions;
using CrmSuite.Modules.Identity.Api.Authorization;
using CrmSuite.Modules.Orders.Application.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CrmSuite.Modules.Orders.Api.Controllers;

[ApiController]
[Route("api/customers/{customerId:guid}/services")]
[Authorize]
[HasPermission(Permissions.Orders.Order.Read)]
public sealed class CustomerServicesController : ControllerBase
{
    private readonly IMediator _mediator;

    public CustomerServicesController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    public async Task<IActionResult> GetByCustomer(
        Guid customerId,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            new GetCustomerServicesQuery(customerId),
            cancellationToken);
        return this.ToApiResult(result);
    }
}
