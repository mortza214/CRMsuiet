namespace CrmSuite.Modules.Foundation.Api.Contracts;

public sealed class UpdateSettingRequest
{
    public string Value { get; init; } = string.Empty;

    public string Group { get; init; } = "General";

    public bool IsSecret { get; init; }
}
