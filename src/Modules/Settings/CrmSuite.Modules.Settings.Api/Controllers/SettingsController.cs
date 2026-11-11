using CrmSuite.BuildingBlocks.Application.Security;
using CrmSuite.Modules.Foundation.Api.Extensions;
using CrmSuite.Modules.Foundation.Api.Filters;
using CrmSuite.Modules.Identity.Api.Authorization;
using CrmSuite.Modules.Settings.Api.Contracts;
using CrmSuite.Modules.Settings.Application.Commands;
using CrmSuite.Modules.Settings.Application.Dtos;
using CrmSuite.Modules.Settings.Application.Queries;
using CrmSuite.Modules.Settings.Application.Sms;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CrmSuite.Modules.Settings.Api.Controllers;

[ApiController]
[Route("api/settings")]
[Authorize]
[Audit]
public sealed class SettingsController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly CrmSuite.Modules.Notifications.Application.Abstractions.INotificationService _notifications;

    public SettingsController(
        IMediator mediator,
        CrmSuite.Modules.Notifications.Application.Abstractions.INotificationService notifications)
    {
        _mediator = mediator;
        _notifications = notifications;
    }

    [HttpPost("notifications/test")]
    [HasPermission(Permissions.Settings.Sms.Manage)]
    public async Task<IActionResult> TestNotification(
        [FromBody] TestNotificationRequest request,
        CancellationToken cancellationToken)
    {
        var result = request.Channel == "Bale"
            ? await _notifications.SendBale(request.Recipient, request.Message, cancellationToken)
            : await _notifications.SendSms(request.Recipient, request.Message, cancellationToken);
        return Ok(CrmSuite.BuildingBlocks.Application.Common.ApiResponse<object>.Ok(
            new { result.Status, result.Content, result.Error },
            result.Error ?? "پیام تست ارسال شد."));
    }

    [HttpGet]
    [HasPermission(Permissions.Settings.Company.Manage)]
    public async Task<IActionResult> GetSettings(
        [FromQuery] string? group = null,
        CancellationToken cancellationToken = default)
    {
        var result = await _mediator.Send(new GetSettingsByGroupQuery(group), cancellationToken);
        return this.ToApiResult(result);
    }

    [HttpPut]
    [HasPermission(Permissions.Settings.Company.Manage)]
    public async Task<IActionResult> UpdateSettings(
        [FromBody] UpdateSettingsRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            new UpdateSettingsCommand(request.Items.Select(item => new SettingUpdateItem(
                item.Key,
                item.Value,
                item.Group,
                item.IsSecret)).ToArray()),
            cancellationToken);

        return this.ToApiResult(result, "تنظیمات ذخیره شد.");
    }

    [HttpGet("tax-rules")]
    [HasPermission(Permissions.Settings.Company.Manage)]
    public async Task<IActionResult> GetTaxRules(CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetTaxRulesQuery(), cancellationToken);
        return this.ToApiResult(result);
    }

    [HttpPost("tax-rules")]
    [HasPermission(Permissions.Settings.Company.Manage)]
    public async Task<IActionResult> CreateTaxRule(
        [FromBody] TaxRuleRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            new CreateTaxRuleCommand(
                request.Name,
                request.Percentage,
                request.IsActive,
                request.EffectiveFromUtc),
            cancellationToken);

        return this.ToApiResult(result, "قانون مالیاتی ایجاد شد.");
    }

    [HttpPut("tax-rules/{id:guid}")]
    [HasPermission(Permissions.Settings.Company.Manage)]
    public async Task<IActionResult> UpdateTaxRule(
        Guid id,
        [FromBody] TaxRuleRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            new UpdateTaxRuleCommand(
                id,
                request.Name,
                request.Percentage,
                request.IsActive,
                request.EffectiveFromUtc),
            cancellationToken);

        return this.ToApiResult(result, "قانون مالیاتی به‌روزرسانی شد.");
    }

    [HttpGet("gateways")]
    [HasPermission(Permissions.Settings.Company.Manage)]
    public async Task<IActionResult> GetGateways(CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetGatewayConfigsQuery(), cancellationToken);
        return this.ToApiResult(result);
    }

    [HttpPut("gateways")]
    [HasPermission(Permissions.Settings.Company.Manage)]
    public async Task<IActionResult> UpdateGateway(
        [FromBody] GatewayConfigRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            new UpdateGatewayConfigCommand(
                request.Provider,
                request.IsActive,
                request.MerchantId,
                request.TerminalId),
            cancellationToken);

        return this.ToApiResult(result, "تنظیمات درگاه ذخیره شد.");
    }

    [HttpGet("sms/ippanel")]
    [HasPermission(Permissions.Settings.Sms.Read)]
    public async Task<IActionResult> GetIppanelSettings(
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            new GetIppanelSettingsQuery(),
            cancellationToken);
        return this.ToApiResult(result);
    }

    [HttpPut("sms/ippanel")]
    [HasPermission(Permissions.Settings.Sms.Manage)]
    public async Task<IActionResult> UpdateIppanelSettings(
        [FromBody] UpdateIppanelSettingsRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            new UpdateIppanelSettingsCommand(
                request.Enabled,
                request.ApiKey,
                request.FromNumber,
                request.PatternCode,
                request.BaseUrl,
                request.PatternParamName),
            cancellationToken);
        return this.ToApiResult(result, "تنظیمات IPPanel ذخیره شد.");
    }

    [HttpPost("sms/ippanel/test")]
    [HasPermission(Permissions.Settings.Sms.Manage)]
    public async Task<IActionResult> TestIppanelSettings(
        [FromBody] TestIppanelSettingsRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            new TestIppanelSettingsCommand(request.Mobile),
            cancellationToken);
        return this.ToApiResult(result);
    }
}
