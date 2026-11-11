namespace CrmSuite.Modules.Customers.Application.Dtos;

public sealed record CustomerOtpRequestDto(
    string Mobile,
    DateTime ExpiresAtUtc,
    string ExpiresAtJalali,
    string? CorrelationId = null);

public sealed record CustomerTokenDto(
    string AccessToken,
    DateTime ExpiresAtUtc,
    string ExpiresAtJalali,
    IReadOnlyList<string> Roles);

public sealed record CustomerAuthResultDto(
    string AccessToken,
    DateTime AccessTokenExpiresAt,
    string AccessTokenExpiresAtJalali,
    CustomerDto Customer);
