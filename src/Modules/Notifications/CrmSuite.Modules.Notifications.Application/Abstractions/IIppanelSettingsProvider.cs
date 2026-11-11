namespace CrmSuite.Modules.Notifications.Application.Abstractions;

public sealed record IppanelRuntimeSettings(
    bool Enabled,
    string ApiKey,
    string FromNumber,
    string PatternCode,
    string BaseUrl,
    string PatternParamName,
    string Source,
    bool IsComplete);

public interface IIppanelSettingsProvider
{
    Task<IppanelRuntimeSettings> GetAsync(CancellationToken cancellationToken);
}
