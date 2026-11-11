namespace CrmSuite.Modules.Identity.Application.Auth;

public sealed class JwtOptions
{
    public const string SectionName = "Jwt";

    public string Issuer { get; set; } = "CrmSuite";

    public string Audience { get; set; } = "CrmSuite.Clients";

    public string Key { get; set; } = string.Empty;

    public int AccessTokenMinutes { get; set; } = 60;

    public int RefreshTokenDays { get; set; } = 30;
}
