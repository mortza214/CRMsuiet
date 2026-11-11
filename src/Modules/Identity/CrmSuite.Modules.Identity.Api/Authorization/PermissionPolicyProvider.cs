using System.Collections.Concurrent;
using CrmSuite.BuildingBlocks.Application.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;

namespace CrmSuite.Modules.Identity.Api.Authorization;

/// <summary>
/// Resolves either a bare permission code (for [Authorize(Policy = ...)]) or the
/// HasPermission:&lt;code&gt; policy emitted by <see cref="HasPermissionAttribute"/>.
/// </summary>
public sealed class PermissionPolicyProvider : IAuthorizationPolicyProvider
{
    private readonly DefaultAuthorizationPolicyProvider _fallback;
    private readonly ConcurrentDictionary<string, AuthorizationPolicy> _policies = new(StringComparer.Ordinal);

    public PermissionPolicyProvider(IOptions<AuthorizationOptions> options)
    {
        _fallback = new DefaultAuthorizationPolicyProvider(options);
    }

    public Task<AuthorizationPolicy?> GetPolicyAsync(string policyName)
    {
        var code = policyName.StartsWith(PermissionPolicyPrefix.Value, StringComparison.Ordinal)
            ? policyName[PermissionPolicyPrefix.Value.Length..]
            : policyName;

        if (!Permissions.IsDefined(code))
        {
            return _fallback.GetPolicyAsync(policyName);
        }

        var policy = _policies.GetOrAdd(
            policyName,
            _ => new AuthorizationPolicyBuilder()
                .RequireAuthenticatedUser()
                .AddRequirements(new PermissionRequirement(code))
                .Build());

        return Task.FromResult<AuthorizationPolicy?>(policy);
    }

    public Task<AuthorizationPolicy> GetDefaultPolicyAsync() => _fallback.GetDefaultPolicyAsync();

    public Task<AuthorizationPolicy?> GetFallbackPolicyAsync() => _fallback.GetFallbackPolicyAsync();
}
