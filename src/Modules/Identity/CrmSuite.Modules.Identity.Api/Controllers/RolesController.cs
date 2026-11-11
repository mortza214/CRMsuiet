using CrmSuite.BuildingBlocks.Application.Common;
using CrmSuite.BuildingBlocks.Application.Security;
using CrmSuite.Modules.Foundation.Api.Extensions;
using CrmSuite.Modules.Foundation.Api.Filters;
using CrmSuite.Modules.Identity.Api.Authorization;
using CrmSuite.Modules.Identity.Api.Contracts;
using CrmSuite.Modules.Identity.Application.Dtos;
using CrmSuite.Modules.Identity.Application.Roles.Commands;
using CrmSuite.Modules.Identity.Application.Roles.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace CrmSuite.Modules.Identity.Api.Controllers;

[ApiController]
[Route("api/roles")]
[Authorize]
[Audit]
public sealed class RolesController : ControllerBase
{
    private readonly IMediator _mediator;

    public RolesController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    [HasPermission(Permissions.Identity.Role.Read)]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<RoleDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetRoles(CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetRolesQuery(), cancellationToken);
        return this.ToApiResult(result);
    }

    [HttpGet("{id:guid}")]
    [HasPermission(Permissions.Identity.Role.Read)]
    [ProducesResponseType(typeof(ApiResponse<RoleDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetRole(Guid id, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetRoleByIdQuery(id), cancellationToken);
        return this.ToApiResult(result);
    }

    [HttpPost]
    [HasPermission(Permissions.Identity.Role.Manage)]
    [ProducesResponseType(typeof(ApiResponse<RoleDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> CreateRole(
        [FromBody] CreateRoleRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            new CreateRoleCommand(request.Name, request.DisplayName, request.PermissionIds),
            cancellationToken);

        return this.ToApiResult(result, "نقش با موفقیت ایجاد شد.");
    }

    [HttpPut("{id:guid}")]
    [HasPermission(Permissions.Identity.Role.Manage)]
    [ProducesResponseType(typeof(ApiResponse<RoleDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> UpdateRole(
        Guid id,
        [FromBody] UpdateRoleRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            new UpdateRoleCommand(id, request.Name, request.DisplayName, request.PermissionIds),
            cancellationToken);

        return this.ToApiResult(result, "نقش با موفقیت به‌روزرسانی شد.");
    }

    [HttpDelete("{id:guid}")]
    [HasPermission(Permissions.Identity.Role.Manage)]
    [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status200OK)]
    public async Task<IActionResult> DeleteRole(Guid id, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new DeleteRoleCommand(id), cancellationToken);
        return this.ToApiResult(result);
    }

    [HttpPut("{id:guid}/permissions")]
    [HasPermission(Permissions.Identity.Role.Manage)]
    [ProducesResponseType(typeof(ApiResponse<RoleDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> AssignPermissions(
        Guid id,
        [FromBody] AssignPermissionsRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            new AssignPermissionsCommand(id, request.PermissionIds),
            cancellationToken);

        return this.ToApiResult(result, "Permissionهای نقش به‌روزرسانی شد.");
    }
}
