using CrmSuite.BuildingBlocks.Application.Common;
using CrmSuite.BuildingBlocks.Application.Security;
using CrmSuite.BuildingBlocks.Domain.Results;
using CrmSuite.Modules.Foundation.Api.Extensions;
using CrmSuite.Modules.Foundation.Api.Filters;
using CrmSuite.Modules.Identity.Api.Authorization;
using CrmSuite.Modules.Orders.Api.Contracts;
using CrmSuite.Modules.Orders.Application.Commands;
using CrmSuite.Modules.Orders.Application.Dtos;
using CrmSuite.Modules.Orders.Application.Queries;
using CrmSuite.Modules.Orders.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace CrmSuite.Modules.Orders.Api.Controllers;

[ApiController]
[Route("api/orders")]
[Authorize]
[HasPermission(Permissions.Orders.Order.Manage)]
[Audit]
public sealed class OrdersController : ControllerBase
{
    private readonly IMediator _mediator;

    public OrdersController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    [HasPermission(Permissions.Orders.Order.Read)]
    [ProducesResponseType(typeof(ApiResponse<PagedResult<OrderListItemDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetOrders(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] OrderStatus? status = null,
        [FromQuery] Guid? customerId = null,
        [FromQuery] Guid? salesExpertId = null,
        [FromQuery] DateTime? fromUtc = null,
        [FromQuery] DateTime? toUtc = null,
        [FromQuery] string? search = null,
        CancellationToken cancellationToken = default)
    {
        var result = await _mediator.Send(
            new GetOrdersQuery(page, pageSize, status, customerId, salesExpertId, fromUtc, toUtc, search),
            cancellationToken);

        return this.ToApiResult(result);
    }

    [HttpGet("{id:guid}")]
    [HasPermission(Permissions.Orders.Order.Read)]
    [ProducesResponseType(typeof(ApiResponse<OrderDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetOrder(Guid id, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetOrderByIdQuery(id), cancellationToken);
        return this.ToApiResult(result);
    }

    [HttpPost]
    [HasPermission(Permissions.Orders.Order.Create)]
    [ProducesResponseType(typeof(ApiResponse<OrderDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> CreateOrder(
        [FromBody] CreateOrderRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            new CreateOrderCommand(
                request.CustomerId,
                request.OrderType,
                request.OrderDateUtc,
                request.Notes,
                request.Items.Select(item => new OrderItemInput(
                    item.ServiceId,
                    item.DataCenterItemId,
                    item.AddonId,
                    item.Quantity,
                    item.BillingCycle,
                    item.AttributesJson,
                    item.Addons
                        .Select(addon => new OrderAddonInput(addon.AddonId, addon.Quantity))
                        .ToArray())).ToArray()),
            cancellationToken);

        return this.ToApiResult(result, "سفارش با موفقیت ایجاد شد.");
    }

    [HttpPut("{id:guid}/status")]
    [HasPermission(Permissions.Orders.Order.Update)]
    [ProducesResponseType(typeof(ApiResponse<OrderDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> UpdateStatus(
        Guid id,
        [FromBody] UpdateOrderStatusRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            new UpdateOrderStatusCommand(id, request.Status, request.Note),
            cancellationToken);

        return this.ToApiResult(result, "وضعیت سفارش تغییر کرد.");
    }

    [HttpPost("{id:guid}/discount")]
    [HasPermission(Permissions.Orders.Sale.Manage)]
    [ProducesResponseType(typeof(ApiResponse<OrderDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ApplyDiscount(
        Guid id,
        [FromBody] ApplyDiscountRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            new ApplyDiscountCommand(id, request.DiscountAmount),
            cancellationToken);

        return this.ToApiResult(result, "تخفیف اعمال شد.");
    }

    [HttpPost("{id:guid}/cancel")]
    [HasPermission(Permissions.Orders.Order.Cancel)]
    [ProducesResponseType(typeof(ApiResponse<OrderDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> CancelOrder(
        Guid id,
        [FromBody] CancelOrderRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new CancelOrderCommand(id, request.Note), cancellationToken);
        return this.ToApiResult(result, "سفارش لغو شد.");
    }
}
