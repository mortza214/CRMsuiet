namespace CrmSuite.Modules.Notifications.Application.Abstractions;

public sealed record SmsResult(
    bool IsSuccess,
    string? MessageId,
    string? ErrorMessage,
    string? RawResponse);

public interface ISmsProvider
{
    Task<SmsResult> SendPatternAsync(
        string patternCode,
        string fromNumber,
        string recipient,
        Dictionary<string, string> parameters,
        CancellationToken cancellationToken);
}
