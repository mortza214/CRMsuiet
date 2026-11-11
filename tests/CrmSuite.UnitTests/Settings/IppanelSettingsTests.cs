using CrmSuite.BuildingBlocks.Domain.Results;
using CrmSuite.Modules.Foundation.Application.Abstractions;
using CrmSuite.Modules.Foundation.Application.Auditing;
using CrmSuite.Modules.Foundation.Application.Dtos;
using CrmSuite.Modules.Notifications.Application.Abstractions;
using CrmSuite.Modules.Settings.Application.Abstractions;
using CrmSuite.Modules.Settings.Application.Sms;
using CrmSuite.Modules.Settings.Infrastructure.Sms;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using FoundationCurrentUser = CrmSuite.Modules.Foundation.Application.Abstractions.ICurrentUserService;

namespace CrmSuite.UnitTests.Settings;

public sealed class IppanelSettingsTests
{
    [Fact]
    public async Task Environment_values_override_database_values()
    {
        var settings = new Mock<ISettingService>();
        settings.Setup(service => service.GetAsync(
                It.IsAny<string>(),
                true,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((string key, bool _includeSecret, CancellationToken _cancellationToken) =>
                key switch
                {
                    IppanelSettingKeys.Enabled => Dto(key, "false"),
                    IppanelSettingKeys.ApiKey => Dto(key, "encrypted-db-key"),
                    IppanelSettingKeys.FromNumber => Dto(key, "DB-FROM"),
                    IppanelSettingKeys.PatternCode => Dto(key, "DB-PATTERN"),
                    IppanelSettingKeys.BaseUrl => Dto(key, "https://db.example/v1/api/send"),
                    IppanelSettingKeys.PatternParamName => Dto(key, "db_param"),
                    _ => null,
                });

        var encryption = new Mock<IEncryptionService>();
        encryption.Setup(service => service.Decrypt(It.IsAny<string>())).Returns("db-key");
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Sms:Ippanel:Enabled"] = "true",
                ["Sms:Ippanel:ApiKey"] = "env-key",
                ["Sms:Ippanel:FromNumber"] = "ENV-FROM",
            })
            .Build();
        var provider = new IppanelSettingsProvider(
            settings.Object,
            encryption.Object,
            configuration,
            NullLogger<IppanelSettingsProvider>.Instance);

        var result = await provider.GetAsync(CancellationToken.None);

        result.Enabled.Should().BeTrue();
        result.ApiKey.Should().Be("env-key");
        result.FromNumber.Should().Be("ENV-FROM");
        result.PatternCode.Should().Be("DB-PATTERN");
        result.Source.Should().Be("Environment");
        encryption.Verify(service => service.Decrypt("encrypted-db-key"), Times.Never);
    }

    [Fact]
    public async Task Get_returns_only_masked_api_key()
    {
        var provider = new Mock<IIppanelSettingsProvider>();
        provider.Setup(service => service.GetAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new IppanelRuntimeSettings(
                true,
                "abcdef1234",
                "+983000000001",
                "PATTERN",
                "https://edge.ippanel.com/v1/api/send",
                "code",
                "Database",
                true));
        var handler = new GetIppanelSettingsQueryHandler(provider.Object);

        var result = await handler.Handle(new GetIppanelSettingsQuery(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.ApiKeyMasked.Should().Be("******1234");
        result.Value.ApiKeyMasked.Should().NotBe("abcdef1234");
    }

    [Fact]
    public async Task Update_with_empty_api_key_preserves_previous_key()
    {
        var settings = new Mock<ISettingService>();
        settings.Setup(service => service.SetAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<bool>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((string key, string value, string _group, bool secret, CancellationToken _cancellationToken) =>
                Result.Success(Dto(key, value, secret)));
        var encryption = new Mock<IEncryptionService>();
        var provider = new Mock<IIppanelSettingsProvider>();
        provider.Setup(service => service.GetAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(CompleteSettings());
        var audit = new Mock<IAuditService>();
        var user = new Mock<FoundationCurrentUser>();
        user.SetupGet(current => current.UserId).Returns(Guid.NewGuid());

        var handler = new UpdateIppanelSettingsCommandHandler(
            settings.Object,
            encryption.Object,
            provider.Object,
            audit.Object,
            user.Object);

        await handler.Handle(
            new UpdateIppanelSettingsCommand(
                true,
                " ",
                "+983000000001",
                "PATTERN",
                "https://edge.ippanel.com/v1/api/send",
                "code"),
            CancellationToken.None);

        encryption.Verify(service => service.Encrypt(It.IsAny<string>()), Times.Never);
        settings.Verify(service => service.SetAsync(
            IppanelSettingKeys.ApiKey,
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<bool>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Update_with_new_api_key_stores_encrypted_value()
    {
        var settings = new Mock<ISettingService>();
        settings.Setup(service => service.SetAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<bool>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((string key, string value, string _group, bool secret, CancellationToken _cancellationToken) =>
                Result.Success(Dto(key, value, secret)));
        var encryption = new Mock<IEncryptionService>();
        encryption.Setup(service => service.Encrypt("new-secret")).Returns("cipher-text");
        var provider = new Mock<IIppanelSettingsProvider>();
        provider.Setup(service => service.GetAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(CompleteSettings());
        var audit = new Mock<IAuditService>();
        var user = new Mock<FoundationCurrentUser>();
        user.SetupGet(current => current.UserId).Returns(Guid.NewGuid());

        var handler = new UpdateIppanelSettingsCommandHandler(
            settings.Object,
            encryption.Object,
            provider.Object,
            audit.Object,
            user.Object);

        await handler.Handle(
            new UpdateIppanelSettingsCommand(
                true,
                "new-secret",
                "+983000000001",
                "PATTERN",
                "https://edge.ippanel.com/v1/api/send",
                "code"),
            CancellationToken.None);

        settings.Verify(service => service.SetAsync(
            IppanelSettingKeys.ApiKey,
            "cipher-text",
            IppanelSettingKeys.Group,
            true,
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Test_with_incomplete_settings_returns_clear_failure_without_exception()
    {
        var provider = new Mock<IIppanelSettingsProvider>();
        provider.Setup(service => service.GetAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new IppanelRuntimeSettings(
                true,
                string.Empty,
                string.Empty,
                string.Empty,
                "https://edge.ippanel.com/v1/api/send",
                "code",
                "Environment",
                false));
        var sms = new Mock<ISmsProvider>();
        var handler = new TestIppanelSettingsCommandHandler(provider.Object, sms.Object);

        var result = await handler.Handle(
            new TestIppanelSettingsCommand("09121234567"),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.IsSuccess.Should().BeFalse();
        result.Value.ErrorMessage.Should().Contain("ناقص");
        sms.Verify(service => service.SendPatternAsync(
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<Dictionary<string, string>>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    private static SettingDto Dto(string key, string value, bool isSecret = false) =>
        new(
            key,
            value,
            IppanelSettingKeys.Group,
            isSecret,
            DateTime.UtcNow,
            "1405/08/18",
            null,
            null);

    private static IppanelRuntimeSettings CompleteSettings() =>
        new(
            true,
            "old-key",
            "+983000000001",
            "PATTERN",
            "https://edge.ippanel.com/v1/api/send",
            "code",
            "Database",
            true);
}
