using CrmSuite.BuildingBlocks.Infrastructure.Persistence;
using CrmSuite.Modules.Settings.Application.Abstractions;
using CrmSuite.Modules.Settings.Domain.Entities;
using CrmSuite.Modules.Settings.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace CrmSuite.Modules.Settings.Infrastructure.Persistence.Repositories;

public sealed class GatewayConfigRepository : IGatewayConfigRepository
{
    private readonly AppDbContext _dbContext;

    public GatewayConfigRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<GatewayConfig?> GetByProviderAsync(
        GatewayProvider provider,
        CancellationToken cancellationToken) =>
        _dbContext.Set<GatewayConfig>()
            .SingleOrDefaultAsync(config => config.Provider == provider, cancellationToken);

    public async Task<IReadOnlyList<GatewayConfig>> GetAllAsync(CancellationToken cancellationToken) =>
        await _dbContext.Set<GatewayConfig>()
            .OrderBy(config => config.Provider)
            .ToListAsync(cancellationToken);

    public void Add(GatewayConfig config) => _dbContext.Set<GatewayConfig>().Add(config);
}
