using CrmSuite.Modules.Settings.Domain.Enums;

namespace CrmSuite.Modules.Settings.Api.Contracts;

public sealed class SettingUpdateRequest
{
    public string Key { get; init; } = string.Empty;

    public string Value { get; init; } = string.Empty;

    public string Group { get; init; } = string.Empty;

    public bool IsSecret { get; init; }
}

public sealed class UpdateSettingsRequest
{
    public List<SettingUpdateRequest> Items { get; init; } = [];
}

public sealed class TaxRuleRequest
{
    public string Name { get; init; } = string.Empty;

    public decimal Percentage { get; init; }

    public bool IsActive { get; init; }

    public DateTime EffectiveFromUtc { get; init; }
}

public sealed class GatewayConfigRequest
{
    public GatewayProvider Provider { get; init; }

    public bool IsActive { get; init; }

    public string MerchantId { get; init; } = string.Empty;

    public string TerminalId { get; init; } = string.Empty;
}
