using CrmSuite.BuildingBlocks.Application.Common;
using CrmSuite.BuildingBlocks.Application.Security;
using CrmSuite.Modules.Customers.Api.Contracts;
using CrmSuite.Modules.Customers.Application.Commands;
using CrmSuite.Modules.Customers.Application.Dtos;
using CrmSuite.Modules.Customers.Application.Queries;
using CrmSuite.Modules.Foundation.Api.Extensions;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace CrmSuite.Modules.Customers.Api.Controllers.Portal;

[ApiController]
[Route("api/portal/customers")]
[Authorize(Roles = Roles.Customer)]
public sealed class CustomersController : ControllerBase
{
    private readonly IMediator _mediator;

    public CustomersController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet("me")]
    [ProducesResponseType(typeof(ApiResponse<CustomerDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMyProfile(CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetMyCustomerProfileQuery(), cancellationToken);
        return this.ToApiResult(result);
    }

    [HttpPut("me")]
    [ProducesResponseType(typeof(ApiResponse<CustomerDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> UpdateMyProfile(
        [FromBody] UpdateMyProfileRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            new UpdateMyCustomerProfileCommand(
                request.Phone,
                request.Email,
                request.Address),
            cancellationToken);

        return this.ToApiResult(result, "پروفایل با موفقیت به‌روزرسانی شد.");
    }

    [HttpGet("my-sub-customers")]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<CustomerDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMySubCustomers(CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetSubCustomersQuery(), cancellationToken);
        return this.ToApiResult(result);
    }
}
