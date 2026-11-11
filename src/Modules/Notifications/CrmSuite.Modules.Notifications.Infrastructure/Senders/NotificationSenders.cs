using System.Net.Http.Json;
using CrmSuite.Modules.Foundation.Application.Abstractions;
using CrmSuite.Modules.Notifications.Application.Abstractions;
using CrmSuite.Modules.Notifications.Domain.Enums;

namespace CrmSuite.Modules.Notifications.Infrastructure.Senders;

public sealed class InAppNotificationSender : INotificationSender
{
    public NotificationChannel Channel => NotificationChannel.InApp;

    public Task SendAsync(string recipient, string content, CancellationToken cancellationToken) =>
        Task.CompletedTask;
}

public sealed class SmsNotificationSender : INotificationSender
{
    private readonly ISmsProvider _provider;

    public SmsNotificationSender(ISmsProvider provider)
    {
        _provider = provider;
    }

    public NotificationChannel Channel => NotificationChannel.Sms;

    public async Task SendAsync(string recipient, string content, CancellationToken cancellationToken)
    {
        var result = await _provider.SendSmsAsync(recipient, content, cancellationToken);
        if (!result.IsSuccess)
        {
            throw new InvalidOperationException(result.ErrorMessage ?? "ارسال پیامک ناموفق بود.");
        }
    }
}

public sealed class BaleNotificationSender : INotificationSender
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ISettingService _settings;

    public BaleNotificationSender(
        IHttpClientFactory httpClientFactory,
        ISettingService settings)
    {
        _httpClientFactory = httpClientFactory;
        _settings = settings;
    }

    public NotificationChannel Channel => NotificationChannel.Bale;

    public async Task SendAsync(string recipient, string content, CancellationToken cancellationToken)
    {
        var token = (await _settings.GetAsync("Bale.Token", true, cancellationToken))?.Value;
        if (string.IsNullOrWhiteSpace(token))
        {
            throw new InvalidOperationException("توکن Bale تنظیم نشده است.");
        }

        var client = _httpClientFactory.CreateClient();
        var url = $"https://tapi.bale.ai/bot{token}/sendMessage";
        using var response = await client.PostAsJsonAsync(
            url,
            new { chat_id = recipient, text = content },
            cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            var raw = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new InvalidOperationException($"Bale HTTP {(int)response.StatusCode}: {raw}");
        }
    }
}
