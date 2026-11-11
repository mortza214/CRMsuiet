using CrmSuite.BuildingBlocks.Application.Security;
using CrmSuite.Modules.Foundation.Api.Extensions;
using CrmSuite.Modules.Foundation.Api.Filters;
using CrmSuite.Modules.Identity.Api.Authorization;
using CrmSuite.Modules.Orders.Api.Contracts;
using CrmSuite.Modules.Orders.Application.Commands;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CrmSuite.Modules.Orders.Api.Controllers;

[ApiController]
[Route("api/orders/{orderId:guid}/items")]
[Authorize]
[Audit]
public sealed class OrderItemsController : ControllerBase
{
    private readonly IMediator _mediator;

    public OrderItemsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost]
    [HasPermission(Permissions.Orders.Order.Create)]
    public async Task<IActionResult> AddItem(
        Guid orderId,
        [FromBody] AddOrderItemRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            new AddOrderItemCommand(
                orderId,
                request.ServiceId,
                request.Quantity,
                request.BillingCycle,
                request.AttributesJson,
                request.Addons
                    .Select(addon => new OrderAddonInput(addon.AddonId, addon.Quantity))
                    .ToArray()),
            cancellationToken);
        return this.ToApiResult(result, "آیتم به سفارش اضافه شد.");
    }

    [HttpPut("{itemId:guid}")]
    [HasPermission(Permissions.Orders.Order.Edit)]
    public async Task<IActionResult> UpdateItem(
        Guid orderId,
        Guid itemId,
        [FromBody] UpdateOrderItemAttributesRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            new UpdateOrderItemAttributesCommand(orderId, itemId, request.AttributesJson),
            cancellationToken);
        return this.ToApiResult(result, "فیلدهای آیتم سفارش به‌روزرسانی شد.");
    }

    [HttpPost("{itemId:guid}/override-price")]
    [HasPermission(Permissions.Orders.Order.PriceOverride)]
    public async Task<IActionResult> OverridePrice(
        Guid orderId,
        Guid itemId,
        [FromBody] OverrideOrderItemPriceRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            new OverrideOrderItemPriceCommand(
                orderId,
                itemId,
                request.OverridePrice,
                request.Reason),
            cancellationToken);
        return this.ToApiResult(result, "قیمت آیتم سفارش با ثبت دلیل تغییر کرد.");
    }
}
