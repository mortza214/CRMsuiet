namespace CrmSuite.Modules.Settings.Api.Contracts;

public sealed class UpdateIppanelSettingsRequest
{
    public bool Enabled { get; init; }

    /// <summary>Empty means keep the previously stored key.</summary>
    public string? ApiKey { get; init; }

    public string FromNumber { get; init; } = string.Empty;

    public string PatternCode { get; init; } = string.Empty;

    public string BaseUrl { get; init; } = "https://edge.ippanel.com/v1/api/send";

    public string PatternParamName { get; init; } = "code";
}

public sealed class TestIppanelSettingsRequest
{
    public string Mobile { get; init; } = string.Empty;
}
