using CrmSuite.BuildingBlocks.Domain.Results;
using CrmSuite.Modules.Foundation.Application.Abstractions;
using CrmSuite.Modules.Foundation.Application.Auditing;
using CrmSuite.Modules.Foundation.Domain.Enums;
using CrmSuite.Modules.Notifications.Application.Abstractions;
using CrmSuite.Modules.Settings.Application.Abstractions;
using MediatR;
using FoundationCurrentUser = CrmSuite.Modules.Foundation.Application.Abstractions.ICurrentUserService;

namespace CrmSuite.Modules.Settings.Application.Sms;

public sealed class GetIppanelSettingsQueryHandler
    : IRequestHandler<GetIppanelSettingsQuery, Result<IppanelSettingsDto>>
{
    private readonly IIppanelSettingsProvider _provider;

    public GetIppanelSettingsQueryHandler(IIppanelSettingsProvider provider)
    {
        _provider = provider;
    }

    public async Task<Result<IppanelSettingsDto>> Handle(
        GetIppanelSettingsQuery request,
        CancellationToken cancellationToken)
    {
        var settings = await _provider.GetAsync(cancellationToken);
        return Result.Success(ToDto(settings));
    }

    internal static IppanelSettingsDto ToDto(IppanelRuntimeSettings settings) =>
        new(
            settings.Enabled,
            Mask(settings.ApiKey),
            settings.FromNumber,
            settings.PatternCode,
            settings.BaseUrl,
            settings.PatternParamName,
            settings.Source);

    internal static string? Mask(string? apiKey)
    {
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            return null;
        }

        var trimmed = apiKey.Trim();
        return trimmed.Length <= 4
            ? "******"
            : $"******{trimmed[^4..]}";
    }
}

public sealed class UpdateIppanelSettingsCommandHandler
    : IRequestHandler<UpdateIppanelSettingsCommand, Result<IppanelSettingsDto>>
{
    private readonly ISettingService _settings;
    private readonly IEncryptionService _encryption;
    private readonly IIppanelSettingsProvider _provider;
    private readonly IAuditService _auditService;
    private readonly FoundationCurrentUser _currentUser;

    public UpdateIppanelSettingsCommandHandler(
        ISettingService settings,
        IEncryptionService encryption,
        IIppanelSettingsProvider provider,
        IAuditService auditService,
        FoundationCurrentUser currentUser)
    {
        _settings = settings;
        _encryption = encryption;
        _provider = provider;
        _auditService = auditService;
        _currentUser = currentUser;
    }

    public async Task<Result<IppanelSettingsDto>> Handle(
        UpdateIppanelSettingsCommand request,
        CancellationToken cancellationToken)
    {
        var previous = await _provider.GetAsync(cancellationToken);
        var baseUrl = string.IsNullOrWhiteSpace(request.BaseUrl)
            ? IppanelSettingsProviderDefaults.BaseUrl
            : request.BaseUrl.Trim();
        var paramName = string.IsNullOrWhiteSpace(request.PatternParamName)
            ? IppanelSettingsProviderDefaults.PatternParamName
            : request.PatternParamName.Trim();

        var updates = new List<(string Key, string Value, bool IsSecret)>
        {
            (IppanelSettingKeys.Enabled, request.Enabled ? "true" : "false", false),
            (IppanelSettingKeys.FromNumber, request.FromNumber.Trim(), false),
            (IppanelSettingKeys.PatternCode, request.PatternCode.Trim(), false),
            (IppanelSettingKeys.BaseUrl, baseUrl, false),
            (IppanelSettingKeys.PatternParamName, paramName, false),
        };

        if (!string.IsNullOrWhiteSpace(request.ApiKey))
        {
            updates.Add((
                IppanelSettingKeys.ApiKey,
                _encryption.Encrypt(request.ApiKey.Trim()),
                true));
        }

        foreach (var update in updates)
        {
            var saved = await _settings.SetAsync(
                update.Key,
                update.Value,
                IppanelSettingKeys.Group,
                update.IsSecret,
                cancellationToken);
            if (saved.IsFailure)
            {
                return Result.Failure<IppanelSettingsDto>(saved.Errors, saved.Message);
            }
        }

        var current = await _provider.GetAsync(cancellationToken);

        await _auditService.RecordAsync(
            new AuditEntry(
                "IppanelSettings",
                IppanelSettingKeys.Group,
                AuditAction.Update,
                new
                {
                    previous.Enabled,
                    ApiKey = GetIppanelSettingsQueryHandler.Mask(previous.ApiKey),
                    previous.FromNumber,
                    previous.PatternCode,
                    previous.BaseUrl,
                    previous.PatternParamName,
                    previous.Source,
                },
                new
                {
                    current.Enabled,
                    ApiKey = GetIppanelSettingsQueryHandler.Mask(current.ApiKey),
                    current.FromNumber,
                    current.PatternCode,
                    current.BaseUrl,
                    current.PatternParamName,
                    current.Source,
                },
                _currentUser.UserId),
            cancellationToken);

        return Result.Success(GetIppanelSettingsQueryHandler.ToDto(current));
    }
}

public sealed class TestIppanelSettingsCommandHandler
    : IRequestHandler<TestIppanelSettingsCommand, Result<SmsResult>>
{
    private readonly IIppanelSettingsProvider _provider;
    private readonly ISmsProvider _smsProvider;

    public TestIppanelSettingsCommandHandler(
        IIppanelSettingsProvider provider,
        ISmsProvider smsProvider)
    {
        _provider = provider;
        _smsProvider = smsProvider;
    }

    public async Task<Result<SmsResult>> Handle(
        TestIppanelSettingsCommand request,
        CancellationToken cancellationToken)
    {
        var settings = await _provider.GetAsync(cancellationToken);
        if (!settings.Enabled)
        {
            const string message = "IPPanel غیرفعال است؛ ابتدا آن را فعال کنید.";
            return Result.Success(new SmsResult(false, null, message, null));
        }

        if (!settings.IsComplete)
        {
            const string message =
                "تنظیمات IPPanel ناقص است؛ ApiKey، FromNumber و PatternCode را بررسی کنید.";
            return Result.Success(new SmsResult(false, null, message, null));
        }

        var result = await _smsProvider.SendPatternAsync(
            settings.PatternCode,
            settings.FromNumber,
            request.Mobile.Trim(),
            new Dictionary<string, string>(StringComparer.Ordinal)
            {
                [settings.PatternParamName] = "12345",
            },
            cancellationToken);

        return Result.Success(result);
    }
}

public static class IppanelSettingsProviderDefaults
{
    public const string BaseUrl = "https://edge.ippanel.com/v1/api/send";
    public const string PatternParamName = "code";
}
