using CrmSuite.BuildingBlocks.Application.Common;
using CrmSuite.BuildingBlocks.Application.Security;
using CrmSuite.BuildingBlocks.Domain.Results;
using CrmSuite.Modules.Foundation.Api.Extensions;
using CrmSuite.Modules.Foundation.Application.AuditLogs.Queries;
using CrmSuite.Modules.Foundation.Application.Dtos;
using CrmSuite.Modules.Foundation.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace CrmSuite.Modules.Foundation.Api.Controllers;

[ApiController]
[Route("api/foundation/audit-logs")]
[Authorize(Policy = Permissions.Foundation.AuditLog.Read)]
public sealed class AuditLogsController : ControllerBase
{
    private readonly IMediator _mediator;

    public AuditLogsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<PagedResult<AuditLogDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Get(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? entityName = null,
        [FromQuery] AuditAction? action = null,
        [FromQuery] DateTime? fromUtc = null,
        [FromQuery] DateTime? toUtc = null,
        CancellationToken cancellationToken = default)
    {
        var result = await _mediator.Send(
            new GetAuditLogsQuery(page, pageSize, entityName, action, fromUtc, toUtc),
            cancellationToken);

        return this.ToApiResult(result);
    }
}
