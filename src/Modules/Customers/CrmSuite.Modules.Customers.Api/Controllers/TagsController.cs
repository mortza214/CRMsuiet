using CrmSuite.BuildingBlocks.Application.Common;
using CrmSuite.BuildingBlocks.Application.Security;
using CrmSuite.Modules.Customers.Api.Contracts;
using CrmSuite.Modules.Customers.Application.Commands;
using CrmSuite.Modules.Customers.Application.Dtos;
using CrmSuite.Modules.Customers.Application.Queries;
using CrmSuite.Modules.Foundation.Api.Extensions;
using CrmSuite.Modules.Foundation.Api.Filters;
using CrmSuite.Modules.Identity.Api.Authorization;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace CrmSuite.Modules.Customers.Api.Controllers;

[ApiController]
[Route("api/customer-tags")]
[Authorize]
[Audit]
public sealed class TagsController : ControllerBase
{
    private readonly IMediator _mediator;

    public TagsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    [HasPermission(Permissions.Customers.Tag.Read)]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<CustomerTagDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetTags(CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetCustomerTagsQuery(), cancellationToken);
        return this.ToApiResult(result);
    }

    [HttpPost]
    [HasPermission(Permissions.Customers.Tag.Manage)]
    public async Task<IActionResult> CreateTag(
        [FromBody] CreateCustomerTagRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            new CreateCustomerTagCommand(request.Name, request.Color),
            cancellationToken);

        return this.ToApiResult(result, "تگ مشتری ایجاد شد.");
    }

    [HttpPut("{id:guid}")]
    [HasPermission(Permissions.Customers.Tag.Manage)]
    public async Task<IActionResult> UpdateTag(
        Guid id,
        [FromBody] UpdateCustomerTagRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            new UpdateCustomerTagCommand(id, request.Name, request.Color),
            cancellationToken);

        return this.ToApiResult(result, "تگ مشتری به‌روزرسانی شد.");
    }

    [HttpDelete("{id:guid}")]
    [HasPermission(Permissions.Customers.Tag.Manage)]
    public async Task<IActionResult> DeleteTag(Guid id, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new DeleteCustomerTagCommand(id), cancellationToken);
        return this.ToApiResult(result);
    }
}
