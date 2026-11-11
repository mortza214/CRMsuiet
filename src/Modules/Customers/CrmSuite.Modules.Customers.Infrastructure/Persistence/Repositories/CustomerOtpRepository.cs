using CrmSuite.BuildingBlocks.Infrastructure.Persistence;
using CrmSuite.Modules.Customers.Application.Abstractions;
using CrmSuite.Modules.Customers.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace CrmSuite.Modules.Customers.Infrastructure.Persistence.Repositories;

public sealed class CustomerOtpRepository : ICustomerOtpRepository
{
    private readonly AppDbContext _dbContext;

    public CustomerOtpRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<CustomerOtp?> GetLatestUnusedAsync(
        string mobile,
        CancellationToken cancellationToken)
    {
        var normalized = mobile.Trim();

        return _dbContext.Set<CustomerOtp>()
            .Where(otp => otp.Mobile == normalized && !otp.IsUsed)
            .OrderByDescending(otp => otp.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public Task<CustomerOtp?> GetByCorrelationIdAsync(
        string correlationId,
        CancellationToken cancellationToken)
    {
        var normalized = correlationId.Trim();
        return _dbContext.Set<CustomerOtp>()
            .SingleOrDefaultAsync(
                otp => otp.CorrelationId == normalized,
                cancellationToken);
    }

    public async Task InvalidateActiveAsync(
        string mobile,
        DateTime utcNow,
        CancellationToken cancellationToken)
    {
        var normalized = mobile.Trim();

        await _dbContext.Set<CustomerOtp>()
            .Where(otp => otp.Mobile == normalized && !otp.IsUsed)
            .ExecuteUpdateAsync(
                update => update.SetProperty(otp => otp.IsUsed, true),
                cancellationToken);
    }

    public void Add(CustomerOtp otp) => _dbContext.Set<CustomerOtp>().Add(otp);
}
