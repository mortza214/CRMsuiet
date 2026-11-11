using CrmSuite.BuildingBlocks.Application.Security;
using CrmSuite.Modules.Catalog.Api.Contracts;
using CrmSuite.Modules.Catalog.Application.Queries;
using CrmSuite.Modules.Catalog.Application.Services;
using CrmSuite.Modules.Foundation.Api.Extensions;
using CrmSuite.Modules.Identity.Api.Authorization;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CrmSuite.Modules.Catalog.Api.Controllers;

[ApiController]
[Route("api/pricing")]
[Authorize]
public sealed class PricingController : ControllerBase
{
    private readonly IMediator _mediator;

    public PricingController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost("preview")]
    [HasPermission(Permissions.Catalog.Price.Read)]
    public async Task<IActionResult> Preview(
        [FromBody] PricePreviewRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            new PreviewServicePriceQuery(
                request.ServiceId,
                request.Quantity,
                request.Addons
                    .Select(addon => new AddonSelection(addon.AddonId, addon.Quantity))
                    .ToArray(),
                request.AttributesJson),
            cancellationToken);

        return this.ToApiResult(result);
    }
}
