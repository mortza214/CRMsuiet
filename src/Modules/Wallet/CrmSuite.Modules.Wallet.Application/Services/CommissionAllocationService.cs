using CrmSuite.Modules.Customers.Domain.Enums;

namespace CrmSuite.Modules.Wallet.Application.Services;

public sealed record CommissionShareSpec(
    Guid? UserId,
    decimal Percentage,
    bool IsCompanyShare);

public sealed class CommissionAllocationService
{
    /// <summary>
    /// Phase 8 allocation rules. Percentages always add up to 100.
    /// </summary>
    public IReadOnlyList<CommissionShareSpec> Allocate(
        CustomerOwnerType ownerType,
        Guid? ownerUserId,
        Guid salesExpertId,
        Guid? followUpUserId)
    {
        return ownerType switch
        {
            CustomerOwnerType.CompanyReferred =>
            [
                new CommissionShareSpec(salesExpertId, 65m, false),
                new CommissionShareSpec(null, 35m, true),
            ],
            CustomerOwnerType.CompanyCustomer =>
            [
                new CommissionShareSpec(salesExpertId, 35m, false),
                new CommissionShareSpec(null, 65m, true),
            ],
            CustomerOwnerType.FollowUp =>
                AllocateFollowUp(ownerUserId, salesExpertId, followUpUserId),
            _ => AllocateOwner(ownerUserId, salesExpertId),
        };
    }

    private static IReadOnlyList<CommissionShareSpec> AllocateOwner(
        Guid? ownerUserId,
        Guid salesExpertId)
    {
        var recipient = ownerUserId ?? salesExpertId;
        return [new CommissionShareSpec(recipient, 100m, false)];
    }

    private static IReadOnlyList<CommissionShareSpec> AllocateFollowUp(
        Guid? ownerUserId,
        Guid salesExpertId,
        Guid? followUpUserId)
    {
        var follower = followUpUserId ?? Guid.Empty;
        var hasFollower = follower != Guid.Empty;
        var remaining = hasFollower ? 90m : 100m;
        var followerShare = hasFollower ? 10m : 0m;

        if (ownerUserId.HasValue && ownerUserId.Value != salesExpertId)
        {
            var half = remaining / 2m;
            var specs = new List<CommissionShareSpec>
            {
                new(salesExpertId, half, false),
                new(ownerUserId.Value, remaining - half, false),
            };
            if (hasFollower)
            {
                specs.Add(new CommissionShareSpec(follower, followerShare, false));
            }

            return specs;
        }

        return hasFollower
            ? [new CommissionShareSpec(salesExpertId, remaining, false),
               new CommissionShareSpec(follower, followerShare, false)]
            : [new CommissionShareSpec(salesExpertId, remaining, false)];
    }
}
