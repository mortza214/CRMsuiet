using System.Security.Claims;
using CrmSuite.BuildingBlocks.Application.Abstractions;
using CrmSuite.BuildingBlocks.Application.Security;
using FoundationCurrentUserService = CrmSuite.Modules.Foundation.Application.Abstractions.ICurrentUserService;
using SecurityPermissions = CrmSuite.BuildingBlocks.Application.Security.Permissions;

namespace CrmSuite.Api.Services;

public sealed class CurrentUserService : ICurrentUserService, FoundationCurrentUserService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentUserService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    private ClaimsPrincipal? Principal => _httpContextAccessor.HttpContext?.User;

    public Guid? UserId =>
        Guid.TryParse(Principal?.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : null;

    public Guid? CustomerId =>
        Guid.TryParse(Principal?.FindFirstValue(CustomClaimTypes.CustomerId), out var id) ? id : null;

    public string? UserName =>
        Principal?.FindFirstValue(ClaimTypes.Name)
        ?? Principal?.FindFirstValue(ClaimTypes.NameIdentifier);

    public bool IsAuthenticated => Principal?.Identity?.IsAuthenticated == true;

    public string? IpAddress =>
        _httpContextAccessor.HttpContext?.Connection.RemoteIpAddress?.ToString();

    public IReadOnlyCollection<string> Permissions =>
        Principal?.FindAll(CustomClaimTypes.Permission)
            .Select(claim => claim.Value)
            .Distinct(StringComparer.Ordinal)
            .ToArray() ?? [];

    public bool HasPermission(string permission) =>
        IsAuthenticated
        && (Principal!.IsInRole(Roles.SuperAdmin)
            || Permissions.Contains(SecurityPermissions.Wildcard, StringComparer.Ordinal)
            || Permissions.Contains(permission, StringComparer.Ordinal));
}
