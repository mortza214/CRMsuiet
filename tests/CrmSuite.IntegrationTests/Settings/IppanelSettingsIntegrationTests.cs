using CrmSuite.BuildingBlocks.Application.Abstractions;
using CrmSuite.BuildingBlocks.Infrastructure.Persistence;
using CrmSuite.Modules.Foundation.Application.Abstractions;
using CrmSuite.Modules.Foundation.Application.Auditing;
using CrmSuite.Modules.Foundation.Domain.Entities;
using CrmSuite.Modules.Foundation.Infrastructure.Dates;
using CrmSuite.Modules.Foundation.Infrastructure.Persistence;
using CrmSuite.Modules.Foundation.Infrastructure.Settings;
using CrmSuite.Modules.Notifications.Application.Abstractions;
using CrmSuite.Modules.Settings.Application.Abstractions;
using CrmSuite.Modules.Settings.Application.Sms;
using CrmSuite.Modules.Settings.Infrastructure.Security;
using CrmSuite.Modules.Settings.Infrastructure.Sms;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using FoundationCurrentUser = CrmSuite.Modules.Foundation.Application.Abstractions.ICurrentUserService;

namespace CrmSuite.IntegrationTests.Settings;

public sealed class IppanelSettingsIntegrationTests
{
    private static readonly DateTime Now = new(2026, 11, 9, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task Full_settings_cycle_encrypts_masks_and_tests_pattern()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase($"ippanel-{Guid.NewGuid():N}")
            .Options;
        using var db = new AppDbContext(
            options,
            new IModuleModelConfigurator[] { new FoundationModelConfigurator() });
        db.Database.EnsureCreated();

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Settings:EncryptionKey"] = "integration-test-encryption-key-32-bytes",
            })
            .Build();
        var encryption = new AesEncryptionService(configuration);
        var currentUser = new FakeCurrentUser();
        var clock = new FakeClock();
        var settings = new SettingService(
            db,
            currentUser,
            clock,
            new JalaliDateService());
        var provider = new IppanelSettingsProvider(
            settings,
            encryption,
            configuration,
            NullLogger<IppanelSettingsProvider>.Instance);
        var audit = new FakeAuditService();
        var update = new UpdateIppanelSettingsCommandHandler(
            settings,
            encryption,
            provider,
            audit,
            currentUser);

        var updated = await update.Handle(
            new UpdateIppanelSettingsCommand(
                true,
                "super-secret-api-key",
                "+983000000001",
                "PATTERN-OTP",
                "https://edge.ippanel.com/v1/api/send",
                "otp"),
            CancellationToken.None);

        updated.IsSuccess.Should().BeTrue();
        updated.Value!.Enabled.Should().BeTrue();
        updated.Value.ApiKeyMasked.Should().Be("******-key");
        updated.Value.ApiKeyMasked.Should().NotContain("super-secret");

        var storedKey = await db.Set<Setting>().SingleAsync(
            setting => setting.Key == IppanelSettingKeys.ApiKey);
        storedKey.Value.Should().NotBe("super-secret-api-key");
        encryption.Decrypt(storedKey.Value).Should().Be("super-secret-api-key");

        var get = await new GetIppanelSettingsQueryHandler(provider)
            .Handle(new GetIppanelSettingsQuery(), CancellationToken.None);
        get.IsSuccess.Should().BeTrue();
        get.Value!.ApiKeyMasked.Should().Be("******-key");

        var sms = new CaptureSmsProvider();
        var test = await new TestIppanelSettingsCommandHandler(provider, sms)
            .Handle(new TestIppanelSettingsCommand("09121234567"), CancellationToken.None);

        test.IsSuccess.Should().BeTrue();
        test.Value!.IsSuccess.Should().BeTrue();
        sms.Parameters.Should().ContainKey("otp");
        sms.Parameters!["otp"].Should().Be("12345");
    }

    private sealed class FakeCurrentUser : FoundationCurrentUser
    {
        public Guid? UserId => Guid.NewGuid();
        public string? UserName => "ippanel-test";
        public string? IpAddress => "127.0.0.1";
        public bool IsAuthenticated => true;
    }

    private sealed class FakeClock : IDateTimeProvider
    {
        public DateTime UtcNow => Now;
    }

    private sealed class FakeAuditService : IAuditService
    {
        public Task RecordAsync(AuditEntry entry, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;
    }

    private sealed class CaptureSmsProvider : ISmsProvider
    {
        public Dictionary<string, string>? Parameters { get; private set; }

        public Task<SmsResult> SendPatternAsync(
            string patternCode,
            string fromNumber,
            string recipient,
            Dictionary<string, string> parameters,
            CancellationToken cancellationToken)
        {
            Parameters = parameters;
            return Task.FromResult(new SmsResult(true, "42", null, "{}"));
        }
    }
}
