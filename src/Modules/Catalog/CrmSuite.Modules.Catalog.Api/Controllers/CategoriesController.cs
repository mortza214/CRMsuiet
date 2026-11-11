using CrmSuite.BuildingBlocks.Application.Common;
using CrmSuite.BuildingBlocks.Application.Security;
using CrmSuite.Modules.Catalog.Api.Contracts;
using CrmSuite.Modules.Catalog.Application.Commands;
using CrmSuite.Modules.Catalog.Application.Dtos;
using CrmSuite.Modules.Catalog.Application.Queries;
using CrmSuite.Modules.Foundation.Api.Extensions;
using CrmSuite.Modules.Foundation.Api.Filters;
using CrmSuite.Modules.Identity.Api.Authorization;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace CrmSuite.Modules.Catalog.Api.Controllers;

[ApiController]
[Route("api/catalog/categories")]
[Authorize]
[Audit]
public sealed class CategoriesController : ControllerBase
{
    private readonly IMediator _mediator;

    public CategoriesController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    [HasPermission(Permissions.Catalog.ServiceGroup.Read)]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<ServiceCategoryDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetCategories(CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetCategoriesQuery(), cancellationToken);
        return this.ToApiResult(result);
    }

    [HttpPost]
    [HasPermission(Permissions.Catalog.ServiceGroup.Manage)]
    public async Task<IActionResult> CreateCategory(
        [FromBody] CreateCategoryRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new CreateCategoryCommand(request.Name, request.ParentId), cancellationToken);
        return this.ToApiResult(result, "دسته‌بندی ایجاد شد.");
    }

    [HttpPut("{id:guid}")]
    [HasPermission(Permissions.Catalog.ServiceGroup.Manage)]
    public async Task<IActionResult> UpdateCategory(
        Guid id,
        [FromBody] UpdateCategoryRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new UpdateCategoryCommand(id, request.Name, request.ParentId), cancellationToken);
        return this.ToApiResult(result, "دسته‌بندی به‌روزرسانی شد.");
    }

    [HttpDelete("{id:guid}")]
    [HasPermission(Permissions.Catalog.ServiceGroup.Manage)]
    public async Task<IActionResult> DeleteCategory(Guid id, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new DeleteCategoryCommand(id), cancellationToken);
        return this.ToApiResult(result);
    }
}
