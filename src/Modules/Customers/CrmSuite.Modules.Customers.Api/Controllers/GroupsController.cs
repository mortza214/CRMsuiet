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
[Route("api/customer-groups")]
[Authorize]
[Audit]
public sealed class GroupsController : ControllerBase
{
    private readonly IMediator _mediator;

    public GroupsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    [HasPermission(Permissions.Customers.CustomerGroup.Read)]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<CustomerGroupDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetGroups(CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetCustomerGroupsQuery(), cancellationToken);
        return this.ToApiResult(result);
    }

    [HttpPost]
    [HasPermission(Permissions.Customers.CustomerGroup.Manage)]
    public async Task<IActionResult> CreateGroup(
        [FromBody] CreateCustomerGroupRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            new CreateCustomerGroupCommand(request.Name, request.Description),
            cancellationToken);

        return this.ToApiResult(result, "گروه مشتری ایجاد شد.");
    }

    [HttpPut("{id:guid}")]
    [HasPermission(Permissions.Customers.CustomerGroup.Manage)]
    public async Task<IActionResult> UpdateGroup(
        Guid id,
        [FromBody] UpdateCustomerGroupRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            new UpdateCustomerGroupCommand(id, request.Name, request.Description),
            cancellationToken);

        return this.ToApiResult(result, "گروه مشتری به‌روزرسانی شد.");
    }

    [HttpDelete("{id:guid}")]
    [HasPermission(Permissions.Customers.CustomerGroup.Manage)]
    public async Task<IActionResult> DeleteGroup(Guid id, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new DeleteCustomerGroupCommand(id), cancellationToken);
        return this.ToApiResult(result);
    }
}
