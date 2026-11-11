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
[Route("api/catalog/addons")]
[Authorize]
[Audit]
public sealed class AddonsController : ControllerBase
{
    private readonly IMediator _mediator;

    public AddonsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    [HasPermission(Permissions.Catalog.AddOn.Read)]
    public async Task<IActionResult> GetAddons(CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetAddonsQuery(), cancellationToken);
        return this.ToApiResult(result);
    }

    [HttpPost]
    [HasPermission(Permissions.Catalog.AddOn.Manage)]
    public async Task<IActionResult> CreateAddon(
        [FromBody] CreateAddonRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            new CreateAddonCommand(
                request.Name,
                request.Price,
                request.Unit,
                request.IsOptional,
                request.ServiceId,
                request.Code,
                request.PricingMode,
                request.SortOrder,
                request.Description,
                request.IsActive),
            cancellationToken);

        return this.ToApiResult(result, "آیتم جانبی ایجاد شد.");
    }

    [HttpPut("{id:guid}")]
    [HasPermission(Permissions.Catalog.AddOn.Manage)]
    public async Task<IActionResult> UpdateAddon(
        Guid id,
        [FromBody] UpdateAddonRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            new UpdateAddonCommand(
                id,
                request.Name,
                request.Price,
                request.Unit,
                request.IsOptional,
                request.ServiceId,
                request.Code,
                request.PricingMode,
                request.SortOrder,
                request.Description,
                request.IsActive),
            cancellationToken);

        return this.ToApiResult(result, "آیتم جانبی به‌روزرسانی شد.");
    }

    [HttpPost("{id:guid}/deactivate")]
    [HasPermission(Permissions.Catalog.AddOn.Manage)]
    public async Task<IActionResult> DeactivateAddon(Guid id, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new DeactivateAddonCommand(id), cancellationToken);
        return this.ToApiResult(result, "آیتم جانبی غیرفعال شد.");
    }

    [HttpDelete("{id:guid}")]
    [HasPermission(Permissions.Catalog.AddOn.Manage)]
    public async Task<IActionResult> DeleteAddon(Guid id, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new DeleteAddonCommand(id), cancellationToken);
        return this.ToApiResult(result);
    }
}
