namespace CrmSuite.Modules.Payments.Infrastructure.Gateways;

public sealed class SamanOptions
{
    public const string SectionName = "Saman";

    public bool Enabled { get; set; }

    public bool IsSandbox { get; set; } = true;

    public string BaseUrl { get; set; } = "https://sep.shaparak.ir";

    public string MerchantId { get; set; } = string.Empty;

    public string TerminalId { get; set; } = string.Empty;

    public string CallbackUrl { get; set; } = string.Empty;

    public string RequestPath { get; set; } = "/onlinepg/onlinepg";

    public string VerifyPath { get; set; } = "/Verify";
}
