using CrmSuite.Modules.Customers.Application.Abstractions;
using CrmSuite.Modules.Customers.Domain.Entities;
using CrmSuite.Modules.Wallet.Domain.Enums;
using FoundationCurrentUser = CrmSuite.Modules.Foundation.Application.Abstractions.ICurrentUserService;

namespace CrmSuite.Modules.Wallet.Application.Services;

public sealed record WalletOwner(Guid OwnerId, WalletOwnerType OwnerType, Customer? Customer);

public sealed class WalletOwnerResolver
{
    private readonly ICustomerRepository _customers;
    private readonly FoundationCurrentUser _currentUser;

    public WalletOwnerResolver(
        ICustomerRepository customers,
        FoundationCurrentUser currentUser)
    {
        _customers = customers;
        _currentUser = currentUser;
    }

    public async Task<WalletOwner?> ResolveAsync(CancellationToken cancellationToken)
    {
        if (_currentUser.UserId is null)
        {
            return null;
        }

        var customer = await _customers.GetByUserIdAsync(
            _currentUser.UserId.Value,
            false,
            cancellationToken);

        return customer is not null
            ? new WalletOwner(customer.Id, WalletOwnerType.Customer, customer)
            : new WalletOwner(_currentUser.UserId.Value, WalletOwnerType.Employee, null);
    }
}
