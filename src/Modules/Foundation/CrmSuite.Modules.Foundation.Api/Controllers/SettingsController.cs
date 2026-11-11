using CrmSuite.BuildingBlocks.Application.Common;
using CrmSuite.BuildingBlocks.Application.Security;
using CrmSuite.Modules.Foundation.Api.Contracts;
using CrmSuite.Modules.Foundation.Api.Extensions;
using CrmSuite.Modules.Foundation.Api.Filters;
using CrmSuite.Modules.Foundation.Application.Dtos;
using CrmSuite.Modules.Foundation.Application.Settings.Commands;
using CrmSuite.Modules.Foundation.Application.Settings.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace CrmSuite.Modules.Foundation.Api.Controllers;

[ApiController]
[Route("api/foundation/settings")]
[Authorize(Policy = Permissions.Foundation.Settings.Manage)]
[Audit]
public sealed class SettingsController : ControllerBase
{
    private readonly IMediator _mediator;

    public SettingsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<SettingDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetSettings(
        [FromQuery] string? group,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetSettingsQuery(group), cancellationToken);
        return this.ToApiResult(result);
    }

    [HttpGet("{key}")]
    [ProducesResponseType(typeof(ApiResponse<SettingDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetSetting(
        string key,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetSettingQuery(key), cancellationToken);
        return this.ToApiResult(result);
    }

    [HttpPut("{key}")]
    [ProducesResponseType(typeof(ApiResponse<SettingDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> UpdateSetting(
        string key,
        [FromBody] UpdateSettingRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            new UpdateSettingCommand(key, request.Value, request.Group, request.IsSecret),
            cancellationToken);

        return this.ToApiResult(result, "تنظیمات با موفقیت ذخیره شد.");
    }
}
