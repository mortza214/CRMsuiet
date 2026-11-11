using CrmSuite.BuildingBlocks.Application.Common;
using CrmSuite.BuildingBlocks.Application.Security;
using CrmSuite.BuildingBlocks.Domain.Results;
using CrmSuite.Modules.Foundation.Api.Extensions;
using CrmSuite.Modules.Foundation.Api.Filters;
using CrmSuite.Modules.Identity.Api.Authorization;
using CrmSuite.Modules.Identity.Api.Contracts;
using CrmSuite.Modules.Identity.Application.Dtos;
using CrmSuite.Modules.Identity.Application.Users.Commands;
using CrmSuite.Modules.Identity.Application.Users.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace CrmSuite.Modules.Identity.Api.Controllers;

[ApiController]
[Route("api/users")]
[Authorize]
[Audit]
public sealed class UsersController : ControllerBase
{
    private readonly IMediator _mediator;

    public UsersController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    [HasPermission(Permissions.Identity.User.Read)]
    [ProducesResponseType(typeof(ApiResponse<PagedResult<UserDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetUsers(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? search = null,
        [FromQuery] bool? isActive = null,
        CancellationToken cancellationToken = default)
    {
        var result = await _mediator.Send(
            new GetUsersQuery(page, pageSize, search, isActive),
            cancellationToken);

        return this.ToApiResult(result);
    }

    [HttpGet("me")]
    [ProducesResponseType(typeof(ApiResponse<UserProfileDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetCurrentUser(CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetCurrentUserQuery(), cancellationToken);
        return this.ToApiResult(result);
    }

    [HttpGet("{id:guid}")]
    [HasPermission(Permissions.Identity.User.Read)]
    [ProducesResponseType(typeof(ApiResponse<UserDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetUser(Guid id, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetUserByIdQuery(id), cancellationToken);
        return this.ToApiResult(result);
    }

    [HttpPost]
    [HasPermission(Permissions.Identity.User.Create)]
    [ProducesResponseType(typeof(ApiResponse<UserDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> CreateUser(
        [FromBody] CreateUserRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            new CreateUserCommand(
                request.Username,
                request.Password,
                request.FullName,
                request.Mobile,
                request.Email,
                request.IsActive,
                request.RoleIds,
                request.DepartmentIds),
            cancellationToken);

        return this.ToApiResult(result, "کاربر با موفقیت ایجاد شد.");
    }

    [HttpPut("{id:guid}")]
    [HasPermission(Permissions.Identity.User.Update)]
    [ProducesResponseType(typeof(ApiResponse<UserDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> UpdateUser(
        Guid id,
        [FromBody] UpdateUserRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            new UpdateUserCommand(
                id,
                request.FullName,
                request.Mobile,
                request.Email,
                request.IsActive,
                request.NewPassword,
                request.RoleIds,
                request.DepartmentIds),
            cancellationToken);

        return this.ToApiResult(result, "کاربر با موفقیت به‌روزرسانی شد.");
    }

    [HttpDelete("{id:guid}")]
    [HasPermission(Permissions.Identity.User.Delete)]
    [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status200OK)]
    public async Task<IActionResult> DeleteUser(Guid id, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new DeleteUserCommand(id), cancellationToken);
        return this.ToApiResult(result);
    }

    [HttpPut("{id:guid}/roles")]
    [HasPermission(Permissions.Identity.User.Update)]
    [ProducesResponseType(typeof(ApiResponse<UserDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> AssignRoles(
        Guid id,
        [FromBody] AssignRolesRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            new AssignRolesCommand(id, request.RoleIds),
            cancellationToken);

        return this.ToApiResult(result, "نقش‌های کاربر به‌روزرسانی شد.");
    }
}
