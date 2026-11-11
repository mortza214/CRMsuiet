using CrmSuite.BuildingBlocks.Application.Security;
using CrmSuite.Modules.Forms.Api.Contracts;
using CrmSuite.Modules.Forms.Application.Commands;
using CrmSuite.Modules.Forms.Application.Queries;
using CrmSuite.Modules.Forms.Domain.Enums;
using CrmSuite.Modules.Foundation.Api.Extensions;
using CrmSuite.Modules.Foundation.Api.Filters;
using CrmSuite.Modules.Identity.Api.Authorization;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CrmSuite.Modules.Forms.Api.Controllers;

[ApiController]
[Route("api/forms")]
[Authorize]
[Audit]
public sealed class FormsController : ControllerBase
{
    private readonly IMediator _mediator;

    public FormsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet("templates")]
    [HasPermission(Permissions.Forms.Form.Read)]
    public async Task<IActionResult> GetTemplates(
        [FromQuery] bool includeInactive = false,
        CancellationToken cancellationToken = default)
    {
        var result = await _mediator.Send(
            new GetFormTemplatesQuery(includeInactive),
            cancellationToken);

        return this.ToApiResult(result);
    }

    [HttpGet("templates/by-code/{code}")]
    [HasPermission(Permissions.Forms.Form.Read)]
    public async Task<IActionResult> GetTemplateByCode(
        string code,
        [FromQuery] bool requirePublished = true,
        CancellationToken cancellationToken = default)
    {
        var result = await _mediator.Send(
            new GetFormTemplateByCodeQuery(code, requirePublished),
            cancellationToken);
        return this.ToApiResult(result);
    }

    [HttpGet("templates/{id:guid}")]
    [HasPermission(Permissions.Forms.Form.Read)]
    public async Task<IActionResult> GetTemplate(Guid id, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            new GetFormTemplateByIdQuery(id),
            cancellationToken);
        return this.ToApiResult(result);
    }

    [HttpPost("templates")]
    [HasPermission(Permissions.Forms.Form.Manage)]
    public async Task<IActionResult> CreateTemplate(
        [FromBody] CreateFormTemplateRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            new CreateFormTemplateCommand(
                request.Title,
                request.SchemaJson,
                request.Version,
                request.IsActive,
                request.Code,
                request.EntityType,
                request.TargetServiceId,
                request.TargetServiceCategoryId),
            cancellationToken);

        return this.ToApiResult(result, "فرم ایجاد شد.");
    }

    [HttpPut("templates/{id:guid}")]
    [HasPermission(Permissions.Forms.Form.Manage)]
    public async Task<IActionResult> UpdateTemplate(
        Guid id,
        [FromBody] UpdateFormTemplateRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            new UpdateFormTemplateCommand(
                id,
                request.Code,
                request.Title,
                request.SchemaJson,
                request.EntityType,
                request.TargetServiceId,
                request.TargetServiceCategoryId),
            cancellationToken);

        return this.ToApiResult(result, "پیش‌نویس فرم به‌روزرسانی شد.");
    }

    [HttpPost("templates/{id:guid}/publish")]
    [HasPermission(Permissions.Forms.Form.Manage)]
    public async Task<IActionResult> PublishTemplate(
        Guid id,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            new PublishFormTemplateCommand(id),
            cancellationToken);
        return this.ToApiResult(result, "فرم منتشر شد.");
    }

    [HttpPost("templates/{id:guid}/archive")]
    [HasPermission(Permissions.Forms.Form.Manage)]
    public async Task<IActionResult> ArchiveTemplate(
        Guid id,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            new ArchiveFormTemplateCommand(id),
            cancellationToken);
        return this.ToApiResult(result, "فرم آرشیو شد.");
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
                request.CustomerId,
                request.OrderId,
                request.SubmittedDataJson,
                false,
                request.EntityType,
                request.EntityId),
            cancellationToken);

        return this.ToApiResult(result, "پاسخ فرم ثبت شد.");
    }

    [HttpGet("submissions")]
    [HasPermission(Permissions.Forms.Submission.Read)]
    public async Task<IActionResult> GetSubmissions(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] Guid? formTemplateId = null,
        [FromQuery] Guid? customerId = null,
        [FromQuery] FormEntityType? entityType = null,
        [FromQuery] Guid? entityId = null,
        [FromQuery] FormSubmissionStatus? status = null,
        CancellationToken cancellationToken = default)
    {
        var result = await _mediator.Send(
            new GetFormSubmissionsQuery(
                page,
                pageSize,
                formTemplateId,
                customerId,
                entityType,
                entityId,
                status),
            cancellationToken);

        return this.ToApiResult(result);
    }

    [HttpPost("submissions/{id:guid}/review")]
    [HasPermission(Permissions.Forms.Submission.Review)]
    public async Task<IActionResult> ReviewSubmission(
        Guid id,
        [FromBody] ReviewFormSubmissionRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            new ReviewFormSubmissionCommand(id, request.Approve, request.Note),
            cancellationToken);
        return this.ToApiResult(result, request.Approve ? "پاسخ تأیید شد." : "پاسخ رد شد.");
    }
}
