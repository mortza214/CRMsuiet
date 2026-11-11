using CrmSuite.Modules.Identity.Domain.Entities;

namespace CrmSuite.Modules.Identity.Application.Abstractions;

public interface IRefreshTokenRepository
{
    Task<RefreshToken?> GetByTokenAsync(string token, CancellationToken cancellationToken);

    Task<IReadOnlyList<RefreshToken>> GetActiveByUserAsync(
        Guid userId,
        DateTime utcNow,
        CancellationToken cancellationToken);

    void Add(RefreshToken refreshToken);
}
