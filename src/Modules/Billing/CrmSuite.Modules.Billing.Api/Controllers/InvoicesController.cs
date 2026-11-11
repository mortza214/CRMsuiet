using CrmSuite.BuildingBlocks.Application.Common;
using CrmSuite.BuildingBlocks.Application.Security;
using CrmSuite.BuildingBlocks.Domain.Results;
using CrmSuite.Modules.Billing.Api.Contracts;
using CrmSuite.Modules.Billing.Application.Abstractions;
using CrmSuite.Modules.Billing.Application.Commands;
using CrmSuite.Modules.Billing.Application.Dtos;
using CrmSuite.Modules.Billing.Application.Mapping;
using CrmSuite.Modules.Billing.Application.Queries;
using CrmSuite.Modules.Billing.Domain.Enums;
using CrmSuite.Modules.Foundation.Api.Extensions;
using CrmSuite.Modules.Foundation.Api.Filters;
using CrmSuite.Modules.Identity.Api.Authorization;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace CrmSuite.Modules.Billing.Api.Controllers;

[ApiController]
[Route("api/invoices")]
[Authorize]
[HasPermission(Permissions.Billing.Invoice.Manage)]
[Audit]
public sealed class InvoicesController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly IPdfGenerator _pdfGenerator;
    private readonly BillingDtoMapper _mapper;

    public InvoicesController(
        IMediator mediator,
        IPdfGenerator pdfGenerator,
        BillingDtoMapper mapper)
    {
        _mediator = mediator;
        _pdfGenerator = pdfGenerator;
        _mapper = mapper;
    }

    [HttpGet]
    [HasPermission(Permissions.Billing.Invoice.Read)]
    public async Task<IActionResult> GetInvoices(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] InvoiceStatus? status = null,
        [FromQuery] Guid? customerId = null,
        [FromQuery] DateTime? fromUtc = null,
        [FromQuery] DateTime? toUtc = null,
        [FromQuery] string? search = null,
        CancellationToken cancellationToken = default)
    {
        var result = await _mediator.Send(
            new GetInvoicesQuery(page, pageSize, status, customerId, fromUtc, toUtc, search),
            cancellationToken);

        return this.ToApiResult(result);
    }

    [HttpGet("{id:guid}")]
    [HasPermission(Permissions.Billing.Invoice.Read)]
    public async Task<IActionResult> GetInvoice(Guid id, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetInvoiceByIdQuery(id), cancellationToken);
        return this.ToApiResult(result);
    }

    [HttpPost("from-order/{orderId:guid}")]
    [HasPermission(Permissions.Billing.Invoice.Create)]
    public async Task<IActionResult> GenerateFromOrder(
        Guid orderId,
        [FromBody] GenerateInvoiceRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            new GenerateInvoiceFromOrderCommand(orderId, request.Type, request.DueDateUtc),
            cancellationToken);

        return this.ToApiResult(result, "فاکتور با موفقیت صادر شد.");
    }

    [HttpPut("{id:guid}/status")]
    [HasPermission(Permissions.Billing.Invoice.Issue)]
    public async Task<IActionResult> UpdateStatus(
        Guid id,
        [FromBody] UpdateInvoiceStatusRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new UpdateInvoiceStatusCommand(id, request.Status), cancellationToken);
        return this.ToApiResult(result, "وضعیت فاکتور تغییر کرد.");
    }

    [HttpGet("{id:guid}/pdf")]
    [HasPermission(Permissions.Billing.Invoice.DownloadPdf)]
    public async Task<IActionResult> DownloadPdf(Guid id, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetInvoiceByIdQuery(id), cancellationToken);
        if (result.IsFailure || result.Value is null)
        {
            return this.ToApiResult(result);
        }

        var bytes = _pdfGenerator.GenerateInvoice(_mapper.ToInvoicePdfModel(result.Value));
        return File(bytes, "application/pdf", $"{result.Value.InvoiceNumber}.pdf");
    }
}
