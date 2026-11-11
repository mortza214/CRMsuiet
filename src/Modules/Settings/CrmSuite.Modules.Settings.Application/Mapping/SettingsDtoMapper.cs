using CrmSuite.Modules.Foundation.Application.Abstractions;
using CrmSuite.Modules.Settings.Application.Abstractions;
using CrmSuite.Modules.Settings.Application.Dtos;
using CrmSuite.Modules.Settings.Domain.Entities;
using CrmSuite.Modules.Settings.Domain.Enums;

namespace CrmSuite.Modules.Settings.Application.Mapping;

public sealed class SettingsDtoMapper
{
    private readonly IJalaliDateService _jalaliDate;

    public SettingsDtoMapper(IJalaliDateService jalaliDate)
    {
        _jalaliDate = jalaliDate;
    }

    public TaxRuleDto ToTaxRuleDto(TaxRule rule)
    {
        var effectiveFrom = AsUtc(rule.EffectiveFrom);
        return new TaxRuleDto(
            rule.Id,
            rule.Name,
            rule.Percentage,
            rule.IsActive,
            effectiveFrom,
            _jalaliDate.ToJalali(effectiveFrom));
    }

    public GatewayConfigDto ToGatewayDto(
        GatewayConfig config,
        IEncryptionService encryption)
    {
        return new GatewayConfigDto(
            config.Id,
            config.Provider.ToString(),
            config.IsActive,
            SafeDecrypt(encryption, config.MerchantIdEncrypted),
            SafeDecrypt(encryption, config.TerminalIdEncrypted));
    }

    private static string SafeDecrypt(IEncryptionService encryption, string cipherText)
    {
        try
        {
            return encryption.Decrypt(cipherText);
        }
        catch
        {
            return string.Empty;
        }
    }

    private static DateTime AsUtc(DateTime value) =>
        value.Kind == DateTimeKind.Utc ? value : DateTime.SpecifyKind(value, DateTimeKind.Utc);
}
