using System.Text.RegularExpressions;
using CrmSuite.BuildingBlocks.Application.Security;
using FluentAssertions;

namespace CrmSuite.UnitTests.Foundation;

public sealed class PermissionCatalogTests
{
    private static readonly Regex PermissionShape = new(
        "^[A-Za-z]+\\.[A-Za-z]+\\.[A-Za-z]+$",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    [Fact]
    public void Every_permission_follows_module_entity_action_shape()
    {
        Permissions.All.Should().NotBeEmpty();

        foreach (var permission in Permissions.All)
        {
            PermissionShape.IsMatch(permission).Should().BeTrue(
                $"{permission} must match Module.Entity.Action");
        }
    }

    [Fact]
    public void Every_role_has_a_permission_set()
    {
        foreach (var role in Roles.All)
        {
            RolePermissionMap.IsKnownRole(role).Should().BeTrue($"role {role} must be mapped");
            RolePermissionMap.For(role).Should().NotBeEmpty($"role {role} must have permissions");
        }
    }

    [Fact]
    public void Role_permissions_exist_in_catalog_except_wildcard()
    {
        var all = Permissions.All.ToHashSet(StringComparer.Ordinal);

        foreach (var role in Roles.All)
        {
            foreach (var permission in RolePermissionMap.For(role))
            {
                if (permission == Permissions.Wildcard)
                {
                    continue;
                }

                all.Should().Contain(permission, $"{role} references an unknown permission");
            }
        }
    }

    [Fact]
    public void Only_super_admin_has_wildcard()
    {
        var wildcardRoles = Roles.All
            .Where(role => RolePermissionMap.For(role).Contains(Permissions.Wildcard, StringComparer.Ordinal))
            .ToArray();

        wildcardRoles.Should().Equal(Roles.SuperAdmin);
    }

    [Fact]
    public void Sales_expert_has_no_financial_permissions()
    {
        var permissions = RolePermissionMap.For(Roles.SalesExpert);

        permissions.Should().NotContain(Permissions.Billing.Invoice.Create);
        permissions.Should().NotContain(Permissions.Billing.Invoice.Issue);
        permissions.Should().NotContain(Permissions.Payments.Payment.Reconcile);
        permissions.Should().NotContain(Permissions.Wallet.Withdrawal.Approve);
    }
}
