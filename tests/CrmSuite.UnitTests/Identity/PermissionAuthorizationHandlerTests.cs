using System.Security.Claims;
using CrmSuite.BuildingBlocks.Application.Security;
using CrmSuite.Modules.Identity.Api.Authorization;
using FluentAssertions;
using Microsoft.AspNetCore.Authorization;

namespace CrmSuite.UnitTests.Identity;

public sealed class PermissionAuthorizationHandlerTests
{
    [Fact]
    public async Task SalesExpert_cannot_access_financial_permission()
    {
        var identity = new ClaimsIdentity(
        [
            new Claim(ClaimTypes.Name, "sales"),
            new Claim(ClaimTypes.Role, Roles.SalesExpert),
            new Claim(CustomClaimTypes.Permission, Permissions.Customers.Customer.Read),
        ], authenticationType: "test");

        var context = new AuthorizationHandlerContext(
            [new PermissionRequirement(Permissions.Billing.Invoice.Create)],
            new ClaimsPrincipal(identity),
            resource: null);

        await new PermissionAuthorizationHandler().HandleAsync(context);

        context.HasSucceeded.Should().BeFalse();
    }

    [Fact]
    public async Task Role_permission_claim_grants_access()
    {
        var identity = new ClaimsIdentity(
        [
            new Claim(ClaimTypes.Role, Roles.SalesManager),
            new Claim(CustomClaimTypes.Permission, Permissions.Billing.Invoice.Read),
        ], authenticationType: "test");

        var context = new AuthorizationHandlerContext(
            [new PermissionRequirement(Permissions.Billing.Invoice.Read)],
            new ClaimsPrincipal(identity),
            resource: null);

        await new PermissionAuthorizationHandler().HandleAsync(context);

        context.HasSucceeded.Should().BeTrue();
    }
}
