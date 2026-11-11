using CrmSuite.Modules.Customers.Domain.Enums;
using CrmSuite.Modules.Wallet.Application.Services;
using CrmSuite.Modules.Wallet.Infrastructure.Services;
using FluentAssertions;

namespace CrmSuite.UnitTests.Wallet;

public sealed class CommissionCalculatorTests
{
    [Fact]
    public void Ten_million_at_two_percent_is_16666_per_month()
    {
        var calculator = new CommissionCalculator();

        var monthly = calculator.CalculateMonthly(10_000_000m, 2m, 12);

        monthly.Should().Be(16_666m);
    }

    [Theory]
    [InlineData(CustomerOwnerType.CurrentUser, 100, 0)]
    [InlineData(CustomerOwnerType.CompanyReferred, 65, 35)]
    [InlineData(CustomerOwnerType.CompanyCustomer, 35, 65)]
    public void Standard_owner_modes_allocate_expected_percentages(
        CustomerOwnerType ownerType,
        decimal sellerPercent,
        decimal companyPercent)
    {
        var sellerId = Guid.NewGuid();
        var ownerId = Guid.NewGuid();
        var allocation = new CommissionAllocationService().Allocate(
            ownerType,
            ownerId,
            sellerId,
            null);

        allocation.Should().Contain(share => share.Percentage == sellerPercent);
        allocation.Where(share => share.IsCompanyShare).Sum(share => share.Percentage)
            .Should().Be(companyPercent);
        allocation.Sum(share => share.Percentage).Should().Be(100m);
    }

    [Fact]
    public void Follow_up_mode_gives_ten_percent_to_follower()
    {
        var sellerId = Guid.NewGuid();
        var ownerId = Guid.NewGuid();
        var followerId = Guid.NewGuid();

        var allocation = new CommissionAllocationService().Allocate(
            CustomerOwnerType.FollowUp,
            ownerId,
            sellerId,
            followerId);

        allocation.Should().Contain(share => share.UserId == followerId && share.Percentage == 10m);
        allocation.Sum(share => share.Percentage).Should().Be(100m);
    }
}
