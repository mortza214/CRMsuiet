using CrmSuite.Modules.Customers.Domain.Entities;

namespace CrmSuite.Modules.Customers.Application.Abstractions;

public interface ICustomerOtpRepository
{
    Task<CustomerOtp?> GetLatestUnusedAsync(string mobile, CancellationToken cancellationToken);

    Task<CustomerOtp?> GetByCorrelationIdAsync(
        string correlationId,
        CancellationToken cancellationToken);

    Task InvalidateActiveAsync(string mobile, DateTime utcNow, CancellationToken cancellationToken);

    void Add(CustomerOtp otp);
}
