using CrmSuite.BuildingBlocks.Application.Common;
using CrmSuite.BuildingBlocks.Application.Security;
using CrmSuite.BuildingBlocks.Domain.Results;
using CrmSuite.Modules.Customers.Api.Contracts;
using CrmSuite.Modules.Customers.Application.Commands;
using CrmSuite.Modules.Customers.Application.Dtos;
using CrmSuite.Modules.Customers.Application.Queries;
using CrmSuite.Modules.Foundation.Api.Extensions;
using CrmSuite.Modules.Foundation.Api.Filters;
using CrmSuite.Modules.Identity.Api.Authorization;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace CrmSuite.Modules.Customers.Api.Controllers;

[ApiController]
[Route("api/customers")]
[Authorize]
[HasPermission(Permissions.Customers.Customer.Manage)]
[Audit]
public sealed class CustomersController : ControllerBase
{
    private readonly IMediator _mediator;

    public CustomersController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    [HasPermission(Permissions.Customers.Customer.Read)]
    [ProducesResponseType(typeof(ApiResponse<PagedResult<CustomerListItemDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetCustomers(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? search = null,
        [FromQuery] Guid? groupId = null,
        [FromQuery] Guid? tagId = null,
        [FromQuery] bool? isReseller = null,
        [FromQuery] bool onlyDebtors = false,
        [FromQuery] Guid? parentCustomerId = null,
        CancellationToken cancellationToken = default)
    {
        var result = await _mediator.Send(
            new GetCustomersQuery(page, pageSize, search, groupId, tagId, isReseller, onlyDebtors, parentCustomerId),
            cancellationToken);

        return this.ToApiResult(result);
    }

    [HttpGet("{id:guid}")]
    [HasPermission(Permissions.Customers.Customer.Read)]
    [ProducesResponseType(typeof(ApiResponse<CustomerDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetCustomer(Guid id, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetCustomerByIdQuery(id), cancellationToken);
        return this.ToApiResult(result);
    }

    [HttpPost]
    [HasPermission(Permissions.Customers.Customer.Create)]
    [ProducesResponseType(typeof(ApiResponse<CustomerDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> CreateCustomer(
        [FromBody] CreateCustomerRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            new CreateCustomerCommand(
                request.Type,
                request.FirstName,
                request.LastName,
                request.CompanyName,
                request.EconomicCode,
                request.NationalId,
                request.Mobile,
                request.Phone,
                request.Email,
                request.Address,
                request.OwnerUserId,
                request.ParentCustomerId,
                request.IsReseller,
                request.GroupId,
                request.TagIds,
                request.OwnerType),
            cancellationToken);

        return this.ToApiResult(result, "مشتری با موفقیت ایجاد شد.");
    }

    [HttpPut("{id:guid}")]
    [HasPermission(Permissions.Customers.Customer.Update)]
    [ProducesResponseType(typeof(ApiResponse<CustomerDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> UpdateCustomer(
        Guid id,
        [FromBody] UpdateCustomerRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            new UpdateCustomerCommand(
                id,
                request.Type,
                request.FirstName,
                request.LastName,
                request.CompanyName,
                request.EconomicCode,
                request.NationalId,
                request.Mobile,
                request.Phone,
                request.Email,
                request.Address,
                request.OwnerUserId,
                request.ParentCustomerId,
                request.IsReseller,
                request.GroupId,
                request.TagIds),
            cancellationToken);

        return this.ToApiResult(result, "مشتری با موفقیت به‌روزرسانی شد.");
    }

    [HttpPut("{id:guid}/reseller")]
    [HasPermission(Permissions.Customers.Customer.Update)]
    [ProducesResponseType(typeof(ApiResponse<CustomerDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> SetReseller(
        Guid id,
        [FromBody] SetResellerRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            new SetResellerCommand(id, request.IsReseller, request.ParentCustomerId),
            cancellationToken);

        return this.ToApiResult(result, "وضعیت ریسلر بودن مشتری به‌روزرسانی شد.");
    }

    [HttpDelete("{id:guid}")]
    [HasPermission(Permissions.Customers.Customer.Delete)]
    [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status200OK)]
    public async Task<IActionResult> DeleteCustomer(Guid id, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new DeleteCustomerCommand(id), cancellationToken);
        return this.ToApiResult(result);
    }
}
