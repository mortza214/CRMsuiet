using CrmSuite.BuildingBlocks.Domain.Results;
using CrmSuite.Modules.Customers.Application.Dtos;
using CrmSuite.Modules.Customers.Domain.Entities;

namespace CrmSuite.Modules.Customers.Application.Abstractions;

public interface ICustomerIdentityService
{
    /// <summary>Creates/links the Identity portal user and adds it to the current unit of work.</summary>
    Task<Result<Guid>> EnsurePortalUserAsync(Customer customer, CancellationToken cancellationToken);

    Task<Result<CustomerTokenDto>> IssueTokenAsync(Guid userId, CancellationToken cancellationToken);
}
