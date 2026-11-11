using CrmSuite.BuildingBlocks.Application.Security;
using CrmSuite.Modules.Foundation.Api.Extensions;
using CrmSuite.Modules.Payments.Application.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CrmSuite.Modules.Payments.Api.Controllers.Portal;

[ApiController]
[Route("api/portal/payments")]
[Authorize(Roles = Roles.Customer)]
public sealed class PaymentsController : ControllerBase
{
    private readonly IMediator _mediator;

    public PaymentsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet("transactions")]
    public async Task<IActionResult> GetMyTransactions(CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetCustomerTransactionsQuery(), cancellationToken);
        return this.ToApiResult(result);
    }
}
