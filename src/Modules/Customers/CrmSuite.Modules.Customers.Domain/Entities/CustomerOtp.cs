using CrmSuite.BuildingBlocks.Domain.Exceptions;
using CrmSuite.Modules.Foundation.Domain.Entities;

namespace CrmSuite.Modules.Customers.Domain.Entities;

public sealed class CustomerOtp : BaseEntity
{
    private CustomerOtp()
    {
    }

    public CustomerOtp(
        string mobile,
        string code,
        DateTime expiresAtUtc,
        string? requestedByIp,
        DateTime createdAtUtc,
        string? correlationId = null)
        : base(Guid.NewGuid())
    {
        if (string.IsNullOrWhiteSpace(mobile))
        {
            throw new DomainException("موبایل برای OTP الزامی است.", "CUSTOMERS_OTP_MOBILE_REQUIRED");
        }

        if (string.IsNullOrWhiteSpace(code) || code.Length != 6)
        {
            throw new DomainException("کد OTP باید ۶ رقمی باشد.", "CUSTOMERS_OTP_CODE_INVALID");
        }

        Mobile = mobile.Trim();
        Code = code;
        ExpiresAtUtc = expiresAtUtc;
        RequestedByIp = requestedByIp;
        CorrelationId = string.IsNullOrWhiteSpace(correlationId) ? null : correlationId.Trim();
        MarkCreated(null, createdAtUtc);
    }

    public string Mobile { get; private set; } = string.Empty;

    public string Code { get; private set; } = string.Empty;

    public DateTime ExpiresAtUtc { get; private set; }

    public bool IsUsed { get; private set; }

    public int AttemptCount { get; private set; }

    public string? RequestedByIp { get; private set; }

    public string? CorrelationId { get; private set; }

    public bool IsExpired(DateTime utcNow) => ExpiresAtUtc <= utcNow;

    public void MarkUsed() => IsUsed = true;

    public void RegisterFailedAttempt() => AttemptCount++;
}
