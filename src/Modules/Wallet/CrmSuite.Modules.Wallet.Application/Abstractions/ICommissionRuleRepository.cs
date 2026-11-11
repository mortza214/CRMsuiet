using CrmSuite.Modules.Catalog.Domain.Enums;
using CrmSuite.Modules.Wallet.Domain.Entities;

namespace CrmSuite.Modules.Wallet.Application.Abstractions;

public interface ICommissionRuleRepository
{
    Task<CommissionRule?> GetMatchingAsync(
        Guid? serviceId,
        ServiceType? serviceType,
        CancellationToken cancellationToken);

    void Add(CommissionRule rule);
}
