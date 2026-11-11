using CrmSuite.BuildingBlocks.Infrastructure.Persistence;
using CrmSuite.Modules.Identity.Application.Abstractions;
using CrmSuite.Modules.Identity.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace CrmSuite.Modules.Identity.Infrastructure.Persistence.Repositories;

public sealed class RefreshTokenRepository : IRefreshTokenRepository
{
    private readonly AppDbContext _dbContext;

    public RefreshTokenRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<RefreshToken?> GetByTokenAsync(string token, CancellationToken cancellationToken) =>
        _dbContext.Set<RefreshToken>()
            .SingleOrDefaultAsync(refreshToken => refreshToken.Token == token, cancellationToken);

    public async Task<IReadOnlyList<RefreshToken>> GetActiveByUserAsync(
        Guid userId,
        DateTime utcNow,
        CancellationToken cancellationToken) =>
        await _dbContext.Set<RefreshToken>()
            .Where(token => token.UserId == userId
                            && token.RevokedAt == null
                            && token.ExpiresAt > utcNow)
            .ToListAsync(cancellationToken);

    public void Add(RefreshToken refreshToken) =>
        _dbContext.Set<RefreshToken>().Add(refreshToken);
}
