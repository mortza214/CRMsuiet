namespace CrmSuite.Modules.Identity.Application.Dtos;

public sealed record AuthResultDto(
    string AccessToken,
    DateTime AccessTokenExpiresAt,
    string AccessTokenExpiresAtJalali,
    string RefreshToken,
    DateTime RefreshTokenExpiresAt,
    string RefreshTokenExpiresAtJalali,
    UserProfileDto User);
