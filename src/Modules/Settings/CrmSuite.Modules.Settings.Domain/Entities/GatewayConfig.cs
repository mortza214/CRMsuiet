using CrmSuite.BuildingBlocks.Domain.Exceptions;
using CrmSuite.Modules.Foundation.Domain.Entities;
using CrmSuite.Modules.Settings.Domain.Enums;

namespace CrmSuite.Modules.Settings.Domain.Entities;

public sealed class GatewayConfig : BaseEntity
{
    private GatewayConfig()
    {
    }

    public GatewayConfig(
        GatewayProvider provider,
        bool isActive,
        string merchantIdEncrypted,
        string terminalIdEncrypted,
        Guid? createdByUserId,
        DateTime createdAtUtc)
        : base(Guid.NewGuid())
    {
        Provider = provider;
        IsActive = isActive;
        MerchantIdEncrypted = merchantIdEncrypted;
        TerminalIdEncrypted = terminalIdEncrypted;
        MarkCreated(createdByUserId, createdAtUtc);
    }

    public GatewayProvider Provider { get; private set; }

    public bool IsActive { get; private set; }

    public string MerchantIdEncrypted { get; private set; } = string.Empty;

    public string TerminalIdEncrypted { get; private set; } = string.Empty;

    public void Update(
        bool isActive,
        string merchantIdEncrypted,
        string terminalIdEncrypted,
        Guid? userId,
        DateTime utcNow)
    {
        if (string.IsNullOrWhiteSpace(merchantIdEncrypted)
            || string.IsNullOrWhiteSpace(terminalIdEncrypted))
        {
            throw new DomainException("اطلاعات درگاه الزامی است.", "SETTINGS_GATEWAY_CREDENTIALS_REQUIRED");
        }

        IsActive = isActive;
        MerchantIdEncrypted = merchantIdEncrypted;
        TerminalIdEncrypted = terminalIdEncrypted;
        MarkUpdated(userId, utcNow);
    }
}
