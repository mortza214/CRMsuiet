using CrmSuite.BuildingBlocks.Application.Common;
using CrmSuite.BuildingBlocks.Application.Security;
using CrmSuite.Modules.Foundation.Api.Extensions;
using CrmSuite.Modules.Foundation.Api.Filters;
using CrmSuite.Modules.Identity.Api.Authorization;
using CrmSuite.Modules.Workflow.Api.Contracts;
using CrmSuite.Modules.Workflow.Application.Commands;
using CrmSuite.Modules.Workflow.Application.Dtos;
using CrmSuite.Modules.Workflow.Application.Queries;
using CrmSuite.Modules.Workflow.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CrmSuite.Modules.Workflow.Api.Controllers;

[ApiController]
[Route("api/workflow")]
[Authorize]
[Audit]
public sealed class WorkflowController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly CrmSuite.Modules.Identity.Application.Abstractions.IUserRepository _users;

    public WorkflowController(
        IMediator mediator,
        CrmSuite.Modules.Identity.Application.Abstractions.IUserRepository users)
    {
        _mediator = mediator;
        _users = users;
    }

    [HttpGet("experts")]
    [HasPermission(Permissions.Workflow.Instance.Execute)]
    public async Task<IActionResult> GetTechnicalExperts(
        CancellationToken cancellationToken)
    {
        var users = await _users.GetPagedAsync(1, 500, null, true, cancellationToken);
        return Ok(new
        {
            success = true,
            data = users.Items.Where(user => user.UserDepartments.Any(item => item.Department.Name == "فنی")
                || user.UserRoles.Any(item => item.Role.Name is "TechnicalExpert" or "TechnicalManager")).Select(user => new
            {
                id = user.Id,
                fullName = user.FullName,
                departmentIds = user.UserDepartments.Select(item => item.DepartmentId),
            }),
        });
    }

    [HttpGet("definitions")]
    [HasPermission(Permissions.Workflow.Definition.Read)]
    public async Task<IActionResult> GetDefinitions(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] WorkflowDefinitionStatus? status = null,
        [FromQuery] WorkflowEntityType? entityType = null,
        [FromQuery] string? search = null,
        CancellationToken cancellationToken = default)
    {
        var result = await _mediator.Send(
            new GetWorkflowDefinitionsQuery(page, pageSize, status, entityType, search),
            cancellationToken);
        return this.ToApiResult(result);
    }

    [HttpGet("definitions/{id:guid}")]
    [HasPermission(Permissions.Workflow.Definition.Read)]
    public async Task<IActionResult> GetDefinition(
        Guid id,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            new GetWorkflowDefinitionByIdQuery(id),
            cancellationToken);
        return this.ToApiResult(result);
    }

    [HttpPost("definitions")]
    [HasPermission(Permissions.Workflow.Definition.Manage)]
    public async Task<IActionResult> CreateDefinition(
        [FromBody] CreateWorkflowDefinitionRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            new CreateWorkflowDefinitionCommand(
                request.Code,
                request.Name,
                request.EntityType,
                request.Description),
            cancellationToken);
        return this.ToApiResult(result, "گردش کار ایجاد شد.");
    }

    [HttpPut("definitions/{id:guid}")]
    [HasPermission(Permissions.Workflow.Definition.Manage)]
    public async Task<IActionResult> UpdateDefinition(
        Guid id,
        [FromBody] UpdateWorkflowDefinitionRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            new UpdateWorkflowDraftCommand(id, request.Name, request.Description),
            cancellationToken);
        return this.ToApiResult(result, "پیش‌نویس گردش کار به‌روزرسانی شد.");
    }

    [HttpPost("definitions/{id:guid}/publish")]
    [HasPermission(Permissions.Workflow.Definition.Manage)]
    public async Task<IActionResult> PublishDefinition(
        Guid id,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            new PublishWorkflowDefinitionCommand(id),
            cancellationToken);
        return this.ToApiResult(result, "گردش کار منتشر شد.");
    }

    [HttpPost("definitions/{id:guid}/reopen")]
    [HasPermission(Permissions.Workflow.Definition.Manage)]
    public async Task<IActionResult> ReopenDefinition(
        Guid id,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            new ReopenWorkflowDefinitionCommand(id),
            cancellationToken);
        return this.ToApiResult(result, "گردش کار برای ویرایش مستقیم باز شد.");
    }

    [HttpPost("definitions/{id:guid}/clone")]
    [HasPermission(Permissions.Workflow.Definition.Manage)]
    public async Task<IActionResult> CloneDefinition(
        Guid id,
        [FromBody] CloneWorkflowDefinitionRequest? request,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            new CloneWorkflowDefinitionCommand(id, request?.Code),
            cancellationToken);
        return this.ToApiResult(result, "نسخه پیش‌نویس از گردش کار ایجاد شد.");
    }

    [HttpPost("definitions/{id:guid}/archive")]
    [HasPermission(Permissions.Workflow.Definition.Manage)]
    public async Task<IActionResult> ArchiveDefinition(
        Guid id,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            new ArchiveWorkflowDefinitionCommand(id),
            cancellationToken);
        return this.ToApiResult(result, "گردش کار آرشیو شد.");
    }

    [HttpPost("definitions/{id:guid}/services/{serviceId:guid}")]
    [HasPermission(Permissions.Workflow.Definition.Manage)]
    public async Task<IActionResult> BindService(
        Guid id,
        Guid serviceId,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            new BindServiceWorkflowCommand(id, serviceId),
            cancellationToken);
        return this.ToApiResult(result, "سرویس به گردش کار متصل شد.");
    }

    [HttpPost("definitions/{id:guid}/states")]
    [HasPermission(Permissions.Workflow.Definition.Manage)]
    public async Task<IActionResult> AddState(
        Guid id,
        [FromBody] WorkflowStateRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            new AddWorkflowStateCommand(
                id,
                request.Code,
                request.Name,
                request.StateType,
                request.Order,
                request.IsFinal,
                request.AssigneeType,
                request.AssigneeValue,
                request.FormCode,
                request.Color),
            cancellationToken);
        return this.ToApiResult(result, "وضعیت اضافه شد.");
    }

    [HttpPut("definitions/{id:guid}/states/{stateId:guid}")]
    [HasPermission(Permissions.Workflow.Definition.Manage)]
    public async Task<IActionResult> UpdateState(
        Guid id,
        Guid stateId,
        [FromBody] WorkflowStateRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            new UpdateWorkflowStateCommand(
                id,
                stateId,
                request.Code,
                request.Name,
                request.StateType,
                request.Order,
                request.IsFinal,
                request.AssigneeType,
                request.AssigneeValue,
                request.FormCode,
                request.Color),
            cancellationToken);
        return this.ToApiResult(result, "وضعیت به‌روزرسانی شد.");
    }

    [HttpDelete("definitions/{id:guid}/states/{stateId:guid}")]
    [HasPermission(Permissions.Workflow.Definition.Manage)]
    public async Task<IActionResult> RemoveState(
        Guid id,
        Guid stateId,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            new RemoveWorkflowStateCommand(id, stateId),
            cancellationToken);
        return this.ToApiResult(result, "وضعیت حذف شد.");
    }

    [HttpPost("definitions/{id:guid}/transitions")]
    [HasPermission(Permissions.Workflow.Definition.Manage)]
    public async Task<IActionResult> AddTransition(
        Guid id,
        [FromBody] WorkflowTransitionRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            new AddWorkflowTransitionCommand(
                id,
                request.FromStateId,
                request.ToStateId,
                request.Name,
                request.GuardType,
                request.RequiredPermission,
                request.GuardPluginKey,
                request.RequiresComment,
                request.SortOrder),
            cancellationToken);
        return this.ToApiResult(result, "گذار اضافه شد.");
    }

    [HttpDelete("definitions/{id:guid}/transitions/{transitionId:guid}")]
    [HasPermission(Permissions.Workflow.Definition.Manage)]
    public async Task<IActionResult> RemoveTransition(
        Guid id,
        Guid transitionId,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            new RemoveWorkflowTransitionCommand(id, transitionId),
            cancellationToken);
        return this.ToApiResult(result, "گذار حذف شد.");
    }

    [HttpPost("instances")]
    [HasPermission(Permissions.Workflow.Instance.Start)]
    public async Task<IActionResult> StartInstance(
        [FromBody] StartWorkflowRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            new StartWorkflowCommand(
                request.EntityType,
                request.EntityId,
                request.DefinitionCode,
                request.InitialPayload,
                request.IdempotencyKey,
                request.DefinitionId),
            cancellationToken);
        return this.ToApiResult(result, "گردش کار شروع شد.");
    }

    [HttpGet("instances/{id:guid}")]
    [HasPermission(Permissions.Workflow.Instance.Read)]
    public async Task<IActionResult> GetInstance(
        Guid id,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            new GetWorkflowInstanceQuery(id),
            cancellationToken);
        return this.ToApiResult(result);
    }

    [HttpPost("instances/{id:guid}/transition")]
    [HasPermission(Permissions.Workflow.Instance.Execute)]
    public async Task<IActionResult> ExecuteTransition(
        Guid id,
        [FromBody] ExecuteWorkflowTransitionRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            new ExecuteWorkflowTransitionCommand(
                id,
                request.TransitionId,
                request.Comment,
                request.PayloadJson,
                request.IdempotencyKey),
            cancellationToken);
        return this.ToApiResult(result, "گذار اجرا شد.");
    }

    [HttpPost("instances/{id:guid}/action")]
    [HasPermission(Permissions.Workflow.Instance.Execute)]
    public async Task<IActionResult> ExecuteAction(
        Guid id,
        [FromBody] ExecuteWorkflowActionRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            new ExecuteWorkflowActionCommand(
                id,
                request.TransitionId,
                request.ActionDataJson),
            cancellationToken);
        return this.ToApiResult(result, "اکشن گردش کار اجرا شد.");
    }

    [HttpPost("instances/{id:guid}/cancel")]
    [HasPermission(Permissions.Workflow.Instance.Cancel)]
    public async Task<IActionResult> CancelInstance(
        Guid id,
        [FromBody] CancelWorkflowRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            new CancelWorkflowInstanceCommand(id, request.Reason),
            cancellationToken);
        return this.ToApiResult(result, "گردش کار لغو شد.");
    }

    [HttpGet("tasks/my")]
    [HasPermission(Permissions.Workflow.Task.Read)]
    public async Task<IActionResult> GetMyTasks(
        [FromQuery] WorkflowTaskStatus? status = null,
        [FromQuery] Guid? departmentId = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var result = await _mediator.Send(
            new GetMyWorkflowTasksQuery(status, departmentId, page, pageSize),
            cancellationToken);
        return this.ToApiResult(result);
    }

    [HttpPost("tasks/{id:guid}/reassign")]
    [HasPermission(Permissions.Workflow.Task.Reassign)]
    public async Task<IActionResult> ReassignTask(
        Guid id,
        [FromBody] ReassignWorkflowTaskRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            new ReassignWorkflowTaskCommand(
                id,
                request.UserId,
                request.RoleId,
                request.DepartmentId),
            cancellationToken);
        return this.ToApiResult(result, "تسک ارجاع شد.");
    }

    [HttpGet("instances/by-entity")]
    [HasPermission(Permissions.Workflow.Instance.Read)]
    public async Task<IActionResult> GetInstancesByEntity(
        [FromQuery] WorkflowEntityType entityType,
        [FromQuery] Guid entityId,
        CancellationToken cancellationToken = default)
    {
        var result = await _mediator.Send(
            new GetWorkflowInstancesByEntityQuery(entityType, entityId),
            cancellationToken);
        return this.ToApiResult(result);
    }
}
