namespace CrmSuite.Modules.Foundation.Application.Dtos;

public sealed record SettingDto(
    string Key,
    string? Value,
    string Group,
    bool IsSecret,
    DateTime CreatedAt,
    string CreatedAtJalali,
    DateTime? UpdatedAt,
    string? UpdatedAtJalali);
