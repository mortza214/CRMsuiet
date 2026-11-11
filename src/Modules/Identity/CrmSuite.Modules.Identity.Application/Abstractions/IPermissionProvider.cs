namespace CrmSuite.Modules.Identity.Application.Abstractions;

public interface IPermissionProvider
{
    Task<IReadOnlyList<string>> GetPermissionsAsync(Guid userId, CancellationToken cancellationToken);

    void Invalidate(Guid userId);
}
