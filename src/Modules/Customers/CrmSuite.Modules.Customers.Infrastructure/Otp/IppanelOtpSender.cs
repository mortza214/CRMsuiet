using System.Diagnostics.CodeAnalysis;
using CrmSuite.Modules.Customers.Application.Abstractions;
using CrmSuite.Modules.Notifications.Application.Abstractions;
using Microsoft.Extensions.Logging;

namespace CrmSuite.Modules.Customers.Infrastructure.Otp;

/// <summary>
/// OTP adapter for IPPanel Pattern mode. Configuration is resolved at request time so changes
/// in Settings take effect without restarting the API. Development fallback logs the OTP.
/// </summary>
[SuppressMessage(
    "Performance",
    "CA1848:Use LoggerMessage delegates",
    Justification = "OTP delivery is low-volume; structured logging keeps the adapter simple.")]
public sealed class IppanelOtpSender : IOtpSender
{
    private readonly ISmsProvider _smsProvider;
    private readonly IIppanelSettingsProvider _settingsProvider;
    private readonly ILogger<IppanelOtpSender> _logger;

    public IppanelOtpSender(
        ISmsProvider smsProvider,
        IIppanelSettingsProvider settingsProvider,
        ILogger<IppanelOtpSender> logger)
    {
        _smsProvider = smsProvider;
        _settingsProvider = settingsProvider;
        _logger = logger;
    }

    public async Task SendAsync(string mobile, string code, CancellationToken cancellationToken)
    {
        var settings = await _settingsProvider.GetAsync(cancellationToken);

        if (!settings.Enabled)
        {
            // Development fallback and explicit opt-out.
            _logger.LogWarning("OTP for {Mobile}: {Code}", mobile, code);
            return;
        }

        if (!settings.IsComplete)
        {
            _logger.LogError(
                "IPPanel is enabled but settings are incomplete (source: {Source}).",
                settings.Source);
            throw new InvalidOperationException(
                "تنظیمات IPPanel ناقص است؛ ApiKey، FromNumber و PatternCode را بررسی کنید.");
        }

        var result = await _smsProvider.SendPatternAsync(
            settings.PatternCode,
            settings.FromNumber,
            mobile,
            new Dictionary<string, string>(StringComparer.Ordinal)
            {
                [settings.PatternParamName] = code,
            },
            cancellationToken);

        if (!result.IsSuccess)
        {
            _logger.LogError(
                "IPPanel OTP delivery failed for {Mobile}: {Error}",
                mobile,
                result.ErrorMessage);
            throw new InvalidOperationException(
                result.ErrorMessage ?? "ارسال OTP از طریق IPPanel ناموفق بود.");
        }

        _logger.LogInformation(
            "IPPanel OTP sent for {Mobile}; message id {MessageId}",
            mobile,
            result.MessageId);
    }
}
