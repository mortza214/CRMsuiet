using CrmSuite.BuildingBlocks.Domain.Results;
using CrmSuite.Modules.Notifications.Application.Abstractions;
using MediatR;

namespace CrmSuite.Modules.Settings.Application.Sms;

public static class IppanelSettingKeys
{
    public const string Group = "Sms.Ippanel";
    public const string Enabled = "Sms.Ippanel.Enabled";
    public const string ApiKey = "Sms.Ippanel.ApiKey";
    public const string FromNumber = "Sms.Ippanel.FromNumber";
    public const string PatternCode = "Sms.Ippanel.PatternCode";
    public const string BaseUrl = "Sms.Ippanel.BaseUrl";
    public const string PatternParamName = "Sms.Ippanel.PatternParamName";
}

public sealed record IppanelSettingsDto(
    bool Enabled,
    string? ApiKeyMasked,
    string FromNumber,
    string PatternCode,
    string BaseUrl,
    string PatternParamName,
    string Source);

public sealed record GetIppanelSettingsQuery : IRequest<Result<IppanelSettingsDto>>;

public sealed record UpdateIppanelSettingsCommand(
    bool Enabled,
    string? ApiKey,
    string FromNumber,
    string PatternCode,
    string BaseUrl,
    string PatternParamName) : IRequest<Result<IppanelSettingsDto>>;

public sealed record TestIppanelSettingsCommand(string Mobile) : IRequest<Result<SmsResult>>;
