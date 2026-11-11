namespace CrmSuite.Modules.Settings.Application.Dtos;

public sealed record SettingValueDto(
    string Key,
    string Value,
    string Group,
    bool IsSecret);

public sealed record SettingGroupDto(
    string Group,
    IReadOnlyList<SettingValueDto> Items);

public sealed record SettingUpdateItem(
    string Key,
    string Value,
    string Group,
    bool IsSecret = false);

public sealed record TaxRuleDto(
    Guid Id,
    string Name,
    decimal Percentage,
    bool IsActive,
    DateTime EffectiveFrom,
    string EffectiveFromJalali);

public sealed record GatewayConfigDto(
    Guid Id,
    string Provider,
    bool IsActive,
    string MerchantId,
    string TerminalId);
