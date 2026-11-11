using CrmSuite.BuildingBlocks.Application.Security;
using CrmSuite.Modules.Billing.Application.Abstractions;
using CrmSuite.Modules.Billing.Application.Mapping;
using CrmSuite.Modules.Billing.Application.Queries;
using CrmSuite.Modules.Foundation.Api.Extensions;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CrmSuite.Modules.Billing.Api.Controllers.Portal;

[ApiController]
[Route("api/portal/invoices")]
[Authorize(Roles = Roles.Customer)]
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
    public async Task<IActionResult> GetMyInvoices(CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetCustomerInvoicesQuery(), cancellationToken);
        return this.ToApiResult(result);
    }

    [HttpGet("{id:guid}/pdf")]
    public async Task<IActionResult> DownloadPdf(Guid id, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetCustomerInvoiceByIdQuery(id), cancellationToken);
        if (result.IsFailure || result.Value is null)
        {
            return this.ToApiResult(result);
        }

        var bytes = _pdfGenerator.GenerateInvoice(_mapper.ToInvoicePdfModel(result.Value));
        return File(bytes, "application/pdf", $"{result.Value.InvoiceNumber}.pdf");
    }
}
