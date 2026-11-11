using CrmSuite.BuildingBlocks.Application.Security;
using CrmSuite.Modules.Forms.Api.Contracts;
using CrmSuite.Modules.Forms.Application.Commands;
using CrmSuite.Modules.Foundation.Api.Extensions;
using CrmSuite.Modules.Identity.Api.Authorization;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CrmSuite.Modules.Forms.Api.Controllers.Portal;

[ApiController]
[Route("api/portal/forms")]
[Authorize(Roles = Roles.Customer)]
public sealed class FormsController : ControllerBase
{
    private readonly IMediator _mediator;

    public FormsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost("submit")]
    [HasPermission(Permissions.Forms.Submission.Create)]
    public async Task<IActionResult> Submit(
        [FromBody] SubmitFormRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            new SubmitFormCommand(
                request.FormTemplateId,
                null,
                request.OrderId,
                request.SubmittedDataJson,
                true),
            cancellationToken);

        return this.ToApiResult(result, "پاسخ فرم ثبت شد.");
    }
}
