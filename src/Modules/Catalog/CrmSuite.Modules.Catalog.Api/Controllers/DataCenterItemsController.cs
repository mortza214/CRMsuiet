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
[Route("api/catalog/data-center-items")]
[Authorize]
[Audit]
public sealed class DataCenterItemsController : ControllerBase
{
    private readonly IMediator _mediator;

    public DataCenterItemsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    [HasPermission(Permissions.Catalog.DataCenterItem.Read)]
    public async Task<IActionResult> GetItems(CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetDataCenterItemsQuery(), cancellationToken);
        return this.ToApiResult(result);
    }

    [HttpPost]
    [HasPermission(Permissions.Catalog.DataCenterItem.Manage)]
    public async Task<IActionResult> CreateItem(
        [FromBody] CreateDataCenterItemRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            new CreateDataCenterItemCommand(request.ItemType, request.Name, request.Price, request.Unit),
            cancellationToken);

        return this.ToApiResult(result, "آیتم دیتاسنتر ایجاد شد.");
    }

    [HttpPut("{id:guid}")]
    [HasPermission(Permissions.Catalog.DataCenterItem.Manage)]
    public async Task<IActionResult> UpdateItem(
        Guid id,
        [FromBody] UpdateDataCenterItemRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            new UpdateDataCenterItemCommand(id, request.ItemType, request.Name, request.Price, request.Unit),
            cancellationToken);

        return this.ToApiResult(result, "آیتم دیتاسنتر به‌روزرسانی شد.");
    }

    [HttpDelete("{id:guid}")]
    [HasPermission(Permissions.Catalog.DataCenterItem.Manage)]
    public async Task<IActionResult> DeleteItem(Guid id, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new DeleteDataCenterItemCommand(id), cancellationToken);
        return this.ToApiResult(result);
    }
}
