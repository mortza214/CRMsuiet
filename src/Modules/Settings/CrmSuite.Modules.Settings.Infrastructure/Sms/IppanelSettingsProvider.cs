using System.Diagnostics.CodeAnalysis;
using CrmSuite.Modules.Foundation.Application.Abstractions;
using CrmSuite.Modules.Notifications.Application.Abstractions;
using CrmSuite.Modules.Settings.Application.Abstractions;
using CrmSuite.Modules.Settings.Application.Sms;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace CrmSuite.Modules.Settings.Infrastructure.Sms;

[SuppressMessage(
    "Performance",
    "CA1848:Use LoggerMessage delegates",
    Justification = "Configuration errors are low-volume and need readable context.")]
public sealed class IppanelSettingsProvider : IIppanelSettingsProvider
{
    public const string DefaultBaseUrl = "https://edge.ippanel.com/v1/api/send";
    public const string DefaultPatternParamName = "code";

    private readonly ISettingService _settings;
    private readonly IEncryptionService _encryption;
    private readonly IConfiguration _configuration;
    private readonly ILogger<IppanelSettingsProvider> _logger;

    public IppanelSettingsProvider(
        ISettingService settings,
        IEncryptionService encryption,
        IConfiguration configuration,
        ILogger<IppanelSettingsProvider> logger)
    {
        _settings = settings;
        _encryption = encryption;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<IppanelRuntimeSettings> GetAsync(CancellationToken cancellationToken)
    {
        var dbEnabled = await ReadDbAsync(IppanelSettingKeys.Enabled, cancellationToken);
        var dbApiKey = await ReadDbAsync(IppanelSettingKeys.ApiKey, cancellationToken);
        var dbFrom = await ReadDbAsync(IppanelSettingKeys.FromNumber, cancellationToken);
        var dbPattern = await ReadDbAsync(IppanelSettingKeys.PatternCode, cancellationToken);
        var dbBaseUrl = await ReadDbAsync(IppanelSettingKeys.BaseUrl, cancellationToken);
        var dbParamName = await ReadDbAsync(IppanelSettingKeys.PatternParamName, cancellationToken);

        var envEnabled = ReadEnv("Enabled");
        var envApiKey = ReadEnv("ApiKey");
        var envFrom = ReadEnv("FromNumber");
        var envPattern = ReadEnv("PatternCode");
        var envBaseUrl = ReadEnv("BaseUrl");
        var envParamName = ReadEnv("PatternParamName");
        var hasEnvironment = new[]
            {
                envEnabled,
                envApiKey,
                envFrom,
                envPattern,
                envBaseUrl,
                envParamName,
            }
            .Any(value => !string.IsNullOrWhiteSpace(value));

        var enabled = ParseBool(envEnabled)
                      ?? ParseBool(dbEnabled)
                      ?? false;
        var encryptedApiKey = string.IsNullOrWhiteSpace(envApiKey) ? dbApiKey : null;
        var apiKey = !string.IsNullOrWhiteSpace(envApiKey)
            ? envApiKey!
            : Decrypt(encryptedApiKey);
        var from = FirstNonEmpty(envFrom, dbFrom) ?? string.Empty;
        var pattern = FirstNonEmpty(envPattern, dbPattern) ?? string.Empty;
        var baseUrl = FirstNonEmpty(envBaseUrl, dbBaseUrl, DefaultBaseUrl) ?? DefaultBaseUrl;
        var paramName = FirstNonEmpty(
            envParamName,
            dbParamName,
            DefaultPatternParamName) ?? DefaultPatternParamName;

        var hasDatabase = new[]
            {
                dbEnabled,
                dbApiKey,
                dbFrom,
                dbPattern,
                dbBaseUrl,
                dbParamName,
            }
            .Any(value => !string.IsNullOrWhiteSpace(value));

        var source = hasEnvironment
            ? "Environment"
            : hasDatabase
                ? "Database"
                : "None";

        var isComplete = enabled
                         && !string.IsNullOrWhiteSpace(apiKey)
                         && !string.IsNullOrWhiteSpace(from)
                         && !string.IsNullOrWhiteSpace(pattern)
                         && !string.IsNullOrWhiteSpace(baseUrl);

        if (enabled && !isComplete)
        {
            _logger.LogWarning(
                "Ippanel is enabled but settings are incomplete (source: {Source}).",
                source);
        }

        return new IppanelRuntimeSettings(
            enabled,
            apiKey,
            from,
            pattern,
            baseUrl,
            paramName,
            source,
            isComplete);
    }

    private async Task<string?> ReadDbAsync(
        string key,
        CancellationToken cancellationToken)
    {
        var setting = await _settings.GetAsync(key, includeSecret: true, cancellationToken);
        return setting?.Value;
    }

    private string? ReadEnv(string name)
    {
        var primary = _configuration[$"Sms:Ippanel:{name}"];
        if (!string.IsNullOrWhiteSpace(primary))
        {
            return primary;
        }

        // Backward-compatible with phase 4 environment names.
        var legacy = _configuration[$"Ippanel:{name}"];
        return string.IsNullOrWhiteSpace(legacy) ? null : legacy;
    }

    private string Decrypt(string? cipherText)
    {
        if (string.IsNullOrWhiteSpace(cipherText))
        {
            return string.Empty;
        }

        try
        {
            return _encryption.Decrypt(cipherText);
        }
        catch (Exception exception)
        {
            _logger.LogError(
                exception,
                "Ippanel API key could not be decrypted. Check Settings:EncryptionKey.");
            return string.Empty;
        }
    }

    private static bool? ParseBool(string? value) =>
        bool.TryParse(value, out var parsed) ? parsed : null;

    private static string? FirstNonEmpty(params string?[] values) =>
        values.FirstOrDefault(value => !string.IsNullOrWhiteSpace(value));
}
