using CrmSuite.BuildingBlocks.Application.Security;
using CrmSuite.Modules.Billing.Api.Contracts;
using CrmSuite.Modules.Billing.Application.Abstractions;
using CrmSuite.Modules.Billing.Application.Commands;
using CrmSuite.Modules.Billing.Application.Mapping;
using CrmSuite.Modules.Billing.Application.Queries;
using CrmSuite.Modules.Foundation.Api.Extensions;
using CrmSuite.Modules.Foundation.Api.Filters;
using CrmSuite.Modules.Identity.Api.Authorization;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CrmSuite.Modules.Billing.Api.Controllers;

[ApiController]
[Route("api/contracts")]
[Authorize]
[HasPermission(Permissions.Billing.Contract.Manage)]
[Audit]
public sealed class ContractsController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly IPdfGenerator _pdfGenerator;
    private readonly BillingDtoMapper _mapper;

    public ContractsController(
        IMediator mediator,
        IPdfGenerator pdfGenerator,
        BillingDtoMapper mapper)
    {
        _mediator = mediator;
        _pdfGenerator = pdfGenerator;
        _mapper = mapper;
    }

    [HttpPost]
    [HasPermission(Permissions.Billing.Contract.Create)]
    public async Task<IActionResult> CreateContract(
        [FromBody] GenerateContractRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            new GenerateContractCommand(
                request.CustomerId,
                request.OrderId,
                request.StartDateUtc,
                request.EndDateUtc,
                request.Content),
            cancellationToken);

        return this.ToApiResult(result, "قرارداد ایجاد شد.");
    }

    [HttpGet("{id:guid}")]
    [HasPermission(Permissions.Billing.Contract.Read)]
    public async Task<IActionResult> GetContract(Guid id, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetContractByIdQuery(id), cancellationToken);
        return this.ToApiResult(result);
    }

    [HttpGet("{id:guid}/pdf")]
    [HasPermission(Permissions.Billing.Contract.DownloadPdf)]
    public async Task<IActionResult> DownloadPdf(Guid id, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetContractByIdQuery(id), cancellationToken);
        if (result.IsFailure || result.Value is null)
        {
            return this.ToApiResult(result);
        }

        var bytes = _pdfGenerator.GenerateContract(
            _mapper.ToContractPdfModel(result.Value, result.Value.CustomerName));

        return File(bytes, "application/pdf", $"{result.Value.ContractNumber}.pdf");
    }
}
