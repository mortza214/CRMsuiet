using CrmSuite.Modules.Identity.Domain.Entities;

namespace CrmSuite.Modules.Identity.Application.Abstractions;

public interface IUserRepository
{
    Task<User?> GetByUsernameWithAccessAsync(string username, CancellationToken cancellationToken);

    Task<User?> GetByIdWithAccessAsync(Guid id, CancellationToken cancellationToken);

    Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<(IReadOnlyList<User> Items, int TotalCount)> GetPagedAsync(
        int page,
        int pageSize,
        string? search,
        bool? isActive,
        CancellationToken cancellationToken);

    Task<bool> UsernameExistsAsync(string username, Guid? excludeId, CancellationToken cancellationToken);

    Task<bool> MobileExistsAsync(string mobile, Guid? excludeId, CancellationToken cancellationToken);

    void Add(User user);

    void Remove(User user);
}
