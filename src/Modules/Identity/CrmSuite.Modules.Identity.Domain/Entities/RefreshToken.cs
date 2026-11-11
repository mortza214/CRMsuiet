using CrmSuite.BuildingBlocks.Domain.Entities;
using CrmSuite.BuildingBlocks.Domain.Exceptions;

namespace CrmSuite.Modules.Identity.Domain.Entities;

public sealed class RefreshToken : Entity<Guid>
{
    private RefreshToken()
    {
    }

    public RefreshToken(
        string token,
        Guid userId,
        DateTime expiresAt,
        string? createdByIp,
        DateTime createdAt)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            throw new DomainException("Refresh Token نمی‌تواند خالی باشد.", "IDENTITY_REFRESH_TOKEN_REQUIRED");
        }

        Id = Guid.NewGuid();
        Token = token;
        UserId = userId;
        ExpiresAt = expiresAt;
        CreatedByIp = createdByIp;
        CreatedAt = createdAt;
    }

    public string Token { get; private set; } = string.Empty;

    public Guid UserId { get; private set; }

    public User User { get; private set; } = null!;

    public DateTime ExpiresAt { get; private set; }

    public DateTime? RevokedAt { get; private set; }

    public string? RevokedByIp { get; private set; }

    public string? ReplacedByToken { get; private set; }

    public string? CreatedByIp { get; private set; }

    public DateTime CreatedAt { get; private set; }

    public bool IsActive(DateTime utcNow) =>
        RevokedAt is null && ExpiresAt > utcNow;

    public void Revoke(DateTime utcNow, string? ipAddress, string? replacedByToken = null)
    {
        if (RevokedAt.HasValue)
        {
            return;
        }

        RevokedAt = utcNow;
        RevokedByIp = ipAddress;
        ReplacedByToken = replacedByToken;
    }
}
