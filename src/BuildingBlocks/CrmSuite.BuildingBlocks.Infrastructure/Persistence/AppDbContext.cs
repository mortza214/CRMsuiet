using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace CrmSuite.BuildingBlocks.Infrastructure.Persistence;

/// <summary>
/// The single write-side DbContext for the modular monolith. Module entities and DbSets are
/// added incrementally; each module supplies an <see cref="IModuleModelConfigurator"/>.
/// </summary>
public sealed class AppDbContext : DbContext
{
    private readonly IEnumerable<IModuleModelConfigurator> _moduleConfigurators;

    public AppDbContext(
        DbContextOptions<AppDbContext> options,
        IEnumerable<IModuleModelConfigurator>? moduleConfigurators = null)
        : base(options)
    {
        _moduleConfigurators = moduleConfigurators ?? [];
    }

    // DbSets are intentionally empty in the foundation slice. They are added with each module:
    // public DbSet<User> Users => Set<User>();

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.ReplaceService<IModelCacheKeyFactory, ModuleModelCacheKeyFactory>();
    }

    internal object CreateModelCacheKey(bool designTime)
    {
        var configuratorKey = string.Join(
            "|",
            _moduleConfigurators
                .Select(configurator => configurator.GetType().FullName)
                .Order(StringComparer.Ordinal));
        return new ModuleModelCacheKey(
            GetType().FullName ?? GetType().Name,
            configuratorKey,
            designTime);
    }

    private sealed record ModuleModelCacheKey(
        string ContextType,
        string Configurators,
        bool DesignTime);

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // MySQL 8, utf8mb4 everywhere. Table names are configured in snake_case by module mappings.
        modelBuilder.UseCollation("utf8mb4_persian_ci");
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);

        foreach (var configurator in _moduleConfigurators)
        {
            configurator.Configure(modelBuilder);
        }
    }
}

public sealed class ModuleModelCacheKeyFactory : IModelCacheKeyFactory
{
    public object Create(DbContext context, bool designTime) =>
        context is AppDbContext appDbContext
            ? appDbContext.CreateModelCacheKey(designTime)
            : context.GetType();
}
