using CrmSuite.BuildingBlocks.Infrastructure.Persistence;
using CrmSuite.Modules.Identity.Application.Abstractions;
using CrmSuite.Modules.Identity.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace CrmSuite.Modules.Identity.Infrastructure.Persistence.Repositories;

public sealed class UserRepository : IUserRepository
{
    private readonly AppDbContext _dbContext;

    public UserRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<User?> GetByUsernameWithAccessAsync(
        string username,
        CancellationToken cancellationToken)
    {
        var normalized = username.Trim().ToLowerInvariant();

        return WithAccess()
            .SingleOrDefaultAsync(user => user.Username == normalized, cancellationToken);
    }

    public Task<User?> GetByIdWithAccessAsync(Guid id, CancellationToken cancellationToken) =>
        WithAccess().SingleOrDefaultAsync(user => user.Id == id, cancellationToken);

    public Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        _dbContext.Set<User>().SingleOrDefaultAsync(user => user.Id == id, cancellationToken);

    public async Task<(IReadOnlyList<User> Items, int TotalCount)> GetPagedAsync(
        int page,
        int pageSize,
        string? search,
        bool? isActive,
        CancellationToken cancellationToken)
    {
        var query = WithAccess().AsNoTracking();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(user =>
                user.Username.Contains(term)
                || user.FullName.Contains(term)
                || user.Mobile.Contains(term));
        }

        if (isActive.HasValue)
        {
            query = query.Where(user => user.IsActive == isActive.Value);
        }

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderBy(user => user.Username)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    public Task<bool> UsernameExistsAsync(
        string username,
        Guid? excludeId,
        CancellationToken cancellationToken)
    {
        var normalized = username.Trim().ToLowerInvariant();
        var query = _dbContext.Set<User>().Where(user => user.Username == normalized);

        if (excludeId.HasValue)
        {
            query = query.Where(user => user.Id != excludeId.Value);
        }

        return query.AnyAsync(cancellationToken);
    }

    public Task<bool> MobileExistsAsync(
        string mobile,
        Guid? excludeId,
        CancellationToken cancellationToken)
    {
        var normalized = mobile.Trim();
        var query = _dbContext.Set<User>().Where(user => user.Mobile == normalized);

        if (excludeId.HasValue)
        {
            query = query.Where(user => user.Id != excludeId.Value);
        }

        return query.AnyAsync(cancellationToken);
    }

    public void Add(User user) => _dbContext.Set<User>().Add(user);

    public void Remove(User user) => _dbContext.Set<User>().Remove(user);

    private IQueryable<User> WithAccess() =>
        _dbContext.Set<User>()
            .Include(user => user.UserRoles)
            .ThenInclude(userRole => userRole.Role)
            .Include(user => user.UserDepartments)
            .ThenInclude(userDepartment => userDepartment.Department);
}
