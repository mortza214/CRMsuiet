using CrmSuite.Modules.Customers.Infrastructure.Otp;
using CrmSuite.Modules.Notifications.Application.Abstractions;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;

namespace CrmSuite.UnitTests.Customers;

public sealed class IppanelOtpSenderTests
{
    [Fact]
    public async Task Custom_pattern_parameter_name_is_used_in_sms_payload()
    {
        var sms = new CaptureSmsProvider();
        var settings = new StaticSettingsProvider(new IppanelRuntimeSettings(
            true,
            "key",
            "+983000000001",
            "PATTERN-OTP",
            "https://edge.ippanel.com/v1/api/send",
            "otp",
            "Database",
            true));
        var sender = new IppanelOtpSender(
            sms,
            settings,
            NullLogger<IppanelOtpSender>.Instance);

        await sender.SendAsync("09121234567", "123456", CancellationToken.None);

        sms.Parameters.Should().ContainKey("otp");
        sms.Parameters!["otp"].Should().Be("123456");
        sms.Parameters.Should().NotContainKey("code");
    }

    [Fact]
    public async Task Disabled_settings_keep_logging_fallback_without_sms_call()
    {
        var sms = new CaptureSmsProvider();
        var settings = new StaticSettingsProvider(new IppanelRuntimeSettings(
            false,
            string.Empty,
            string.Empty,
            string.Empty,
            "https://edge.ippanel.com/v1/api/send",
            "code",
            "None",
            false));
        var sender = new IppanelOtpSender(
            sms,
            settings,
            NullLogger<IppanelOtpSender>.Instance);

        await sender.SendAsync("09121234567", "123456", CancellationToken.None);

        sms.CallCount.Should().Be(0);
    }

    private sealed class CaptureSmsProvider : ISmsProvider
    {
        public int CallCount { get; private set; }

        public Dictionary<string, string>? Parameters { get; private set; }

        public Task<SmsResult> SendPatternAsync(
            string patternCode,
            string fromNumber,
            string recipient,
            Dictionary<string, string> parameters,
            CancellationToken cancellationToken)
        {
            CallCount++;
            Parameters = parameters;
            return Task.FromResult(new SmsResult(true, "1", null, "{}"));
        }
    }

    private sealed class StaticSettingsProvider : IIppanelSettingsProvider
    {
        private readonly IppanelRuntimeSettings _settings;

        public StaticSettingsProvider(IppanelRuntimeSettings settings)
        {
            _settings = settings;
        }

        public Task<IppanelRuntimeSettings> GetAsync(CancellationToken cancellationToken) =>
            Task.FromResult(_settings);
    }
}
