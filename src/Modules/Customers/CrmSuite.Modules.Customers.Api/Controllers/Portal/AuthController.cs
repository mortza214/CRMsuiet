using CrmSuite.BuildingBlocks.Application.Common;
using CrmSuite.Modules.Customers.Api.Contracts;
using CrmSuite.Modules.Customers.Application.Commands;
using CrmSuite.Modules.Customers.Application.Dtos;
using CrmSuite.Modules.Foundation.Api.Extensions;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace CrmSuite.Modules.Customers.Api.Controllers.Portal;

[ApiController]
[Route("api/portal/auth")]
[AllowAnonymous]
public sealed class AuthController : ControllerBase
{
    private readonly IMediator _mediator;

    public AuthController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost("request-otp")]
    [ProducesResponseType(typeof(ApiResponse<CustomerOtpRequestDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> RequestOtp(
        [FromBody] RequestOtpRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            new RequestOtpCommand(request.Mobile, request.CorrelationId),
            cancellationToken);
        return this.ToApiResult(result, "کد ورود ارسال شد.");
    }

    [HttpPost("verify-otp")]
    [ProducesResponseType(typeof(ApiResponse<CustomerAuthResultDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> VerifyOtp(
        [FromBody] VerifyOtpRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            new VerifyOtpCommand(request.Mobile, request.Code),
            cancellationToken);

        return this.ToApiResult(result, "ورود با موفقیت انجام شد.");
    }
}
