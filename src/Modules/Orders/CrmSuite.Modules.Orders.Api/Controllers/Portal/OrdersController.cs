using CrmSuite.BuildingBlocks.Application.Common;
using CrmSuite.BuildingBlocks.Application.Security;
using CrmSuite.Modules.Foundation.Api.Extensions;
using CrmSuite.Modules.Orders.Application.Dtos;
using CrmSuite.Modules.Orders.Application.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace CrmSuite.Modules.Orders.Api.Controllers.Portal;

[ApiController]
[Route("api/portal/orders")]
[Authorize(Roles = Roles.Customer)]
public sealed class OrdersController : ControllerBase
{
    private readonly IMediator _mediator;

    public OrdersController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<OrderListItemDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMyOrders(CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetCustomerOrdersQuery(), cancellationToken);
        return this.ToApiResult(result);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<OrderDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMyOrder(Guid id, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetMyOrderByIdQuery(id), cancellationToken);
        return this.ToApiResult(result);
    }
}
