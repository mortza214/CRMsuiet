namespace CrmSuite.Modules.Identity.Application.Auth;

public sealed record AccessTokenResult(string AccessToken, DateTime ExpiresAtUtc);

public sealed record RefreshTokenResult(string Token, DateTime ExpiresAtUtc);

public sealed record TokenPair(
    AccessTokenResult AccessToken,
    RefreshTokenResult RefreshToken);
