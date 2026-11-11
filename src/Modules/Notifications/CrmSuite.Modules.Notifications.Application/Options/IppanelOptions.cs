namespace CrmSuite.Modules.Notifications.Application.Options;

public sealed class IppanelOptions
{
    public const string SectionName = "Ippanel";

    /// <summary>Disabled by default; Development uses the logging OTP sender.</summary>
    public bool Enabled { get; set; }

    public string ApiKey { get; set; } = string.Empty;

    public string FromNumber { get; set; } = string.Empty;

    public string PatternCode { get; set; } = string.Empty;

    public string BaseUrl { get; set; } = "https://edge.ippanel.com";
}
