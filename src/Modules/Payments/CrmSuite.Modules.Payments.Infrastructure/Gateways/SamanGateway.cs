using System.Globalization;
using System.Net.Http.Json;
using System.Text.Json;
using CrmSuite.Modules.Payments.Application.Abstractions;
using Microsoft.Extensions.Options;

namespace CrmSuite.Modules.Payments.Infrastructure.Gateways;

public sealed class SamanGateway : ISamanGateway
{
    private readonly HttpClient _httpClient;
    private readonly SamanOptions _options;

    public SamanGateway(HttpClient httpClient, IOptions<SamanOptions> options)
    {
        _httpClient = httpClient;
        _options = options.Value;
    }

    public async Task<SamanPaymentRequestResult> RequestPaymentAsync(
        decimal amount,
        Guid invoiceId,
        string callbackUrl,
        CancellationToken cancellationToken)
    {
        var requestPayload = JsonSerializer.Serialize(new
        {
            amount,
            invoiceId,
            callbackUrl,
            merchantId = _options.MerchantId,
            terminalId = _options.TerminalId,
            sandbox = _options.IsSandbox,
        });

        if (_options.IsSandbox)
        {
            var token = $"SMN-{Guid.NewGuid():N}";
            var paymentUrl = $"{_options.BaseUrl.TrimEnd('/')}/sandbox/pay?token={token}";

            return new SamanPaymentRequestResult(
                true,
                token,
                paymentUrl,
                requestPayload,
                JsonSerializer.Serialize(new { status = 1, token, url = paymentUrl, sandbox = true }),
                null);
        }

        if (!_options.Enabled)
        {
            return new SamanPaymentRequestResult(
                false,
                null,
                null,
                requestPayload,
                null,
                "درگاه سامان غیرفعال است.");
        }

        try
        {
            using var content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["Amount"] = decimal.Truncate(amount).ToString(CultureInfo.InvariantCulture),
                ["ResNum"] = invoiceId.ToString("N"),
                ["RedirectUrl"] = callbackUrl,
                ["TerminalNumber"] = _options.TerminalId,
            });

            using var response = await _httpClient.PostAsync(
                $"{_options.BaseUrl.TrimEnd('/')}{_options.RequestPath}",
                content,
                cancellationToken);

            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            using var document = JsonDocument.Parse(body);
            var root = document.RootElement;
            var token = root.TryGetProperty("token", out var tokenElement)
                ? tokenElement.GetString()
                : null;
            var url = root.TryGetProperty("url", out var urlElement)
                ? urlElement.GetString()
                : null;

            return !string.IsNullOrWhiteSpace(token)
                ? new SamanPaymentRequestResult(true, token, url, requestPayload, body, null)
                : new SamanPaymentRequestResult(false, null, null, requestPayload, body, "درگاه توکن بازنگرداند.");
        }
        catch (Exception exception)
        {
            return new SamanPaymentRequestResult(
                false,
                null,
                null,
                requestPayload,
                null,
                exception.Message);
        }
    }

    public async Task<SamanVerifyResult> VerifyPaymentAsync(
        string token,
        decimal amount,
        CancellationToken cancellationToken)
    {
        if (_options.IsSandbox)
        {
            var reference = $"REF-{Guid.NewGuid():N}"[..20].ToUpperInvariant();

            return new SamanVerifyResult(
                true,
                reference,
                JsonSerializer.Serialize(new
                {
                    status = 1,
                    token,
                    amount,
                    reference,
                    sandbox = true,
                }),
                null);
        }

        if (!_options.Enabled)
        {
            return new SamanVerifyResult(false, null, null, "درگاه سامان غیرفعال است.");
        }

        try
        {
            var payload = new
            {
                RefNum = token,
                TerminalNumber = _options.TerminalId,
                Amount = decimal.Truncate(amount),
            };

            using var response = await _httpClient.PostAsJsonAsync(
                $"{_options.BaseUrl.TrimEnd('/')}{_options.VerifyPath}",
                payload,
                cancellationToken);

            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            using var document = JsonDocument.Parse(body);
            var root = document.RootElement;
            var success = root.TryGetProperty("success", out var successElement)
                && successElement.ValueKind == JsonValueKind.True;
            var reference = root.TryGetProperty("refNum", out var refElement)
                ? refElement.GetString()
                : null;

            return success
                ? new SamanVerifyResult(true, reference, body, null)
                : new SamanVerifyResult(false, null, body, "تراکنش توسط درگاه تأیید نشد.");
        }
        catch (Exception exception)
        {
            return new SamanVerifyResult(false, null, null, exception.Message);
        }
    }
}
