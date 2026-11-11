using CrmSuite.BuildingBlocks.Application.Common;
using CrmSuite.BuildingBlocks.Application.Security;
using CrmSuite.BuildingBlocks.Domain.Results;
using CrmSuite.Modules.Catalog.Api.Contracts;
using CrmSuite.Modules.Catalog.Application.Commands;
using CrmSuite.Modules.Catalog.Application.Dtos;
using CrmSuite.Modules.Catalog.Application.Queries;
using CrmSuite.Modules.Catalog.Domain.Enums;
using CrmSuite.Modules.Foundation.Api.Extensions;
using CrmSuite.Modules.Foundation.Api.Filters;
using CrmSuite.Modules.Identity.Api.Authorization;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace CrmSuite.Modules.Catalog.Api.Controllers;

[ApiController]
[Route("api/catalog/services")]
[Authorize]
[Audit]
public sealed class ServicesController : ControllerBase
{
    private readonly IMediator _mediator;

    public ServicesController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    [HasPermission(Permissions.Catalog.Service.Read)]
    [ProducesResponseType(typeof(ApiResponse<PagedResult<ServiceListItemDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetServices(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? search = null,
        [FromQuery] Guid? categoryId = null,
        [FromQuery] ServiceType? serviceType = null,
        [FromQuery] bool? isActive = null,
        [FromQuery] PricingMode? pricingMode = null,
        CancellationToken cancellationToken = default)
    {
        var result = await _mediator.Send(
            new GetServicesQuery(page, pageSize, search, categoryId, serviceType, isActive, pricingMode),
            cancellationToken);

        return this.ToApiResult(result);
    }

    [HttpGet("{id:guid}")]
    [HasPermission(Permissions.Catalog.Service.Read)]
    [ProducesResponseType(typeof(ApiResponse<ServiceDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetService(Guid id, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetServiceByIdQuery(id), cancellationToken);
        return this.ToApiResult(result);
    }

    [HttpGet("{id:guid}/price")]
    [HasPermission(Permissions.Catalog.Price.Read)]
    public async Task<IActionResult> GetEffectivePrice(
        Guid id,
        [FromQuery] DateTime? date = null,
        CancellationToken cancellationToken = default)
    {
        var result = await _mediator.Send(new GetEffectivePriceQuery(id, date), cancellationToken);
        return this.ToApiResult(result);
    }

    [HttpGet("{id:guid}/prices")]
    [HasPermission(Permissions.Catalog.Price.Read)]
    public async Task<IActionResult> GetPriceHistory(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var result = await _mediator.Send(
            new GetServicePriceHistoryQuery(id),
            cancellationToken);
        return this.ToApiResult(result);
    }

    [HttpGet("{id:guid}/addons")]
    [HasPermission(Permissions.Catalog.AddOn.Read)]
    public async Task<IActionResult> GetServiceAddons(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var result = await _mediator.Send(
            new GetAvailableAddonsForServiceQuery(id),
            cancellationToken);
        return this.ToApiResult(result);
    }

    [HttpGet("{id:guid}/forms")]
    [HasPermission(Permissions.Catalog.Service.Read)]
    public async Task<IActionResult> GetServiceForms(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var result = await _mediator.Send(new GetServiceFormsQuery(id), cancellationToken);
        return this.ToApiResult(result);
    }

    [HttpGet("{id:guid}/form-requirements")]
    [HasPermission(Permissions.Catalog.Service.Read)]
    public async Task<IActionResult> GetServiceFormRequirements(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var result = await _mediator.Send(
            new GetServiceFormRequirementsQuery(id),
            cancellationToken);
        return this.ToApiResult(result);
    }

    [HttpPost]
    [HasPermission(Permissions.Catalog.Service.Manage)]
    public async Task<IActionResult> CreateService(
        [FromBody] CreateServiceRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            new CreateServiceCommand(
                request.Code,
                request.Name,
                request.CategoryId,
                request.ServiceType,
                request.Unit,
                request.Description,
                request.IsActive,
                request.InitialPrice,
                request.PriceEffectiveFromUtc,
                request.PricingMode,
                request.BillingPeriod,
                request.CommissionPercent,
                request.AttributeSchemaJson,
                request.WorkflowDefinitionId),
            cancellationToken);

        return this.ToApiResult(result, "سرویس با موفقیت ایجاد شد.");
    }

    [HttpPut("{id:guid}")]
    [HasPermission(Permissions.Catalog.Service.Manage)]
    public async Task<IActionResult> UpdateService(
        Guid id,
        [FromBody] UpdateServiceRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            new UpdateServiceCommand(
                id,
                request.Code,
                request.Name,
                request.CategoryId,
                request.ServiceType,
                request.Unit,
                request.Description,
                request.IsActive,
                request.PricingMode,
                request.BillingPeriod,
                request.CommissionPercent,
                request.AttributeSchemaJson,
                request.WorkflowDefinitionId),
            cancellationToken);

        return this.ToApiResult(result, "سرویس با موفقیت به‌روزرسانی شد.");
    }

    [HttpPut("{id:guid}/attribute-schema")]
    [HasPermission(Permissions.Catalog.Service.Manage)]
    public async Task<IActionResult> SetAttributeSchema(
        Guid id,
        [FromBody] SetAttributeSchemaRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            new SetServiceAttributeSchemaCommand(id, request.AttributeSchemaJson),
            cancellationToken);
        return this.ToApiResult(result, "AttributeSchema به‌روزرسانی شد.");
    }

    [HttpPost("{id:guid}/activate")]
    [HasPermission(Permissions.Catalog.Service.Manage)]
    public async Task<IActionResult> Activate(Guid id, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new ActivateServiceCommand(id), cancellationToken);
        return this.ToApiResult(result, "سرویس فعال شد.");
    }

    [HttpPost("{id:guid}/deactivate")]
    [HasPermission(Permissions.Catalog.Service.Manage)]
    public async Task<IActionResult> Deactivate(Guid id, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new DeactivateServiceCommand(id), cancellationToken);
        return this.ToApiResult(result, "سرویس غیرفعال شد.");
    }

    [HttpPatch("{id:guid}/status")]
    [HasPermission(Permissions.Catalog.Service.Manage)]
    public async Task<IActionResult> ToggleStatus(
        Guid id,
        [FromBody] ToggleStatusRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            new ToggleServiceStatusCommand(id, request.IsActive),
            cancellationToken);
        return this.ToApiResult(result, "وضعیت سرویس تغییر کرد.");
    }

    [HttpPost("{id:guid}/prices")]
    [HasPermission(Permissions.Catalog.Price.Manage)]
    public async Task<IActionResult> SetPrice(
        Guid id,
        [FromBody] SetServicePriceRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            new SetServicePriceCommand(
                id,
                request.Price,
                request.EffectiveFromUtc,
                request.Currency),
            cancellationToken);

        return this.ToApiResult(result, "قیمت جدید سرویس ثبت شد.");
    }

    [HttpPost("{id:guid}/forms")]
    [HasPermission(Permissions.Catalog.Service.Manage)]
    public async Task<IActionResult> LinkForm(
        Guid id,
        [FromBody] LinkFormRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            new LinkFormToServiceCommand(
                id,
                request.FormDefinitionId,
                request.IsRequired,
                request.SortOrder),
            cancellationToken);
        return this.ToApiResult(result, "فرم به سرویس متصل شد.");
    }

    [HttpDelete("{id:guid}/forms/{formId:guid}")]
    [HasPermission(Permissions.Catalog.Service.Manage)]
    public async Task<IActionResult> UnlinkForm(
        Guid id,
        Guid formId,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            new UnlinkFormFromServiceCommand(id, formId),
            cancellationToken);
        return this.ToApiResult(result, "اتصال فرم حذف شد.");
    }
}

public sealed class ToggleStatusRequest
{
    public bool IsActive { get; init; }
}
