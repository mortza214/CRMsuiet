using System.Net.Http.Json;
using System.Text.Json;
using System.Text.RegularExpressions;
using CrmSuite.Modules.Notifications.Application.Abstractions;

namespace CrmSuite.Modules.Notifications.Infrastructure.Sms;

/// <summary>
/// IPPanel Pattern-only adapter. Configuration is resolved from Environment first and then
/// the encrypted Settings database values through <see cref="IIppanelSettingsProvider"/>.
/// </summary>
public sealed class IppanelSmsProvider : ISmsProvider
{
    private const string PatternPath = "/v1/api/send";

    private readonly HttpClient _httpClient;
    private readonly IIppanelSettingsProvider _settingsProvider;

    public IppanelSmsProvider(
        HttpClient httpClient,
        IIppanelSettingsProvider settingsProvider)
    {
        _httpClient = httpClient;
        _settingsProvider = settingsProvider;
    }

    public async Task<SmsResult> SendSmsAsync(
        string recipient,
        string message,
        CancellationToken cancellationToken)
    {
        var settings = await _settingsProvider.GetAsync(cancellationToken);
        if (!settings.IsComplete || string.IsNullOrWhiteSpace(settings.ApiKey))
        {
            return new SmsResult(false, null, "تنظیمات IPPanel کامل نیست.", null);
        }

        var baseUrl = settings.BaseUrl.TrimEnd('/');
        var url = baseUrl.EndsWith(PatternPath, StringComparison.OrdinalIgnoreCase)
            ? baseUrl
            : $"{baseUrl}{PatternPath}";

        try
        {
            var payload = new
            {
                sending_type = "webservice",
                from_number = settings.FromNumber,
                message,
                recipients = new[] { recipient },
            };
            using var request = new HttpRequestMessage(HttpMethod.Post, url)
            {
                Content = JsonContent.Create(payload),
            };
            request.Headers.TryAddWithoutValidation("Authorization", settings.ApiKey);
            using var response = await _httpClient.SendAsync(request, cancellationToken);
            var raw = await response.Content.ReadAsStringAsync(cancellationToken);
            return response.IsSuccessStatusCode
                ? new SmsResult(true, null, null, raw)
                : new SmsResult(false, null, $"IPPanel HTTP {(int)response.StatusCode}: {raw}", raw);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return new SmsResult(false, null, "زمان پاسخ IPPanel به پایان رسید.", null);
        }
        catch (HttpRequestException exception)
        {
            return new SmsResult(false, null, exception.Message, null);
        }
    }

    public async Task<SmsResult> SendPatternAsync(
        string patternCode,
        string fromNumber,
        string recipient,
        Dictionary<string, string> parameters,
        CancellationToken cancellationToken)
    {
        var settings = await _settingsProvider.GetAsync(cancellationToken);

        if (string.IsNullOrWhiteSpace(recipient))
        {
            return new SmsResult(false, null, "گیرنده SMS خالی است.", null);
        }

        if (!settings.IsComplete)
        {
            return new SmsResult(
                false,
                null,
                "تنظیمات IPPanel کامل نیست (Enabled/ApiKey/FromNumber/PatternCode).",
                null);
        }

        if (string.IsNullOrWhiteSpace(settings.ApiKey))
        {
            return new SmsResult(false, null, "Ippanel API Key تنظیم نشده است.", null);
        }

        var effectivePattern = string.IsNullOrWhiteSpace(patternCode)
            ? settings.PatternCode
            : patternCode;
        var effectiveFrom = string.IsNullOrWhiteSpace(fromNumber)
            ? settings.FromNumber
            : fromNumber;

        var baseUrl = settings.BaseUrl.TrimEnd('/');
        var url = baseUrl.EndsWith(PatternPath, StringComparison.OrdinalIgnoreCase)
            ? baseUrl
            : $"{baseUrl}{PatternPath}";
        var requestParameters = parameters;

        try
        {
            for (var attempt = 0; attempt < 2; attempt++)
            {
                var payload = new
                {
                    sending_type = "pattern",
                    from_number = effectiveFrom,
                    code = effectivePattern,
                    recipients = new[] { recipient },
                    @params = requestParameters,
                };

                using var request = new HttpRequestMessage(HttpMethod.Post, url)
                {
                    Content = JsonContent.Create(payload),
                };
                request.Headers.TryAddWithoutValidation("Authorization", settings.ApiKey);

                using var response = await _httpClient.SendAsync(request, cancellationToken);
                var raw = await response.Content.ReadAsStringAsync(cancellationToken);

                if (!response.IsSuccessStatusCode)
                {
                    // IPPanel is case-sensitive for pattern variables. If it explicitly reports
                    // a missing params.NAME, retry once with that exact name. No SMS is sent on
                    // the failed attempt, so this is safe and keeps the panel usable when the
                    // variable is CODE rather than code.
                    if (attempt == 0
                        && response.StatusCode == System.Net.HttpStatusCode.UnprocessableEntity
                        && TryGetMissingParameterName(raw, out var requiredName)
                        && !requestParameters.ContainsKey(requiredName)
                        && requestParameters.Count == 1)
                    {
                        requestParameters = new Dictionary<string, string>(StringComparer.Ordinal)
                        {
                            [requiredName] = requestParameters.Values.First(),
                        };
                        continue;
                    }

                    return new SmsResult(
                        false,
                        null,
                        $"IPPanel HTTP {(int)response.StatusCode}: {raw}",
                        raw);
                }

                using var document = JsonDocument.Parse(raw);
                var messageId = ReadMessageId(document.RootElement);
                if (string.IsNullOrWhiteSpace(messageId))
                {
                    return new SmsResult(
                        false,
                        null,
                        "IPPanel پاسخ معتبری بدون message_outbox_ids برنگرداند.",
                        raw);
                }

                return new SmsResult(true, messageId, null, raw);
            }
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return new SmsResult(false, null, "زمان پاسخ IPPanel به پایان رسید.", null);
        }
        catch (HttpRequestException exception)
        {
            return new SmsResult(false, null, exception.Message, null);
        }
        catch (JsonException exception)
        {
            return new SmsResult(false, null, $"پاسخ IPPanel JSON معتبری نیست: {exception.Message}", null);
        }

        return new SmsResult(false, null, "ارسال IPPanel انجام نشد.", null);
    }

    private static bool TryGetMissingParameterName(string raw, out string name)
    {
        var match = Regex.Match(
            raw,
            @"params\.([A-Za-z_][A-Za-z0-9_]*)",
            RegexOptions.CultureInvariant);
        name = match.Success ? match.Groups[1].Value : string.Empty;
        return match.Success;
    }

    private static string? ReadMessageId(JsonElement root)
    {
        if (root.TryGetProperty("message_outbox_ids", out var ids))
        {
            if (ids.ValueKind == JsonValueKind.Array && ids.GetArrayLength() > 0)
            {
                return ids[0].ValueKind == JsonValueKind.Number
                    ? ids[0].GetInt64().ToString(System.Globalization.CultureInfo.InvariantCulture)
                    : ids[0].GetString();
            }

            if (ids.ValueKind == JsonValueKind.Number)
            {
                return ids.GetInt64().ToString(System.Globalization.CultureInfo.InvariantCulture);
            }

            if (ids.ValueKind == JsonValueKind.String)
            {
                return ids.GetString();
            }
        }

        if (root.TryGetProperty("data", out var data)
            && data.ValueKind == JsonValueKind.Object)
        {
            return ReadMessageId(data);
        }

        return null;
    }
}
