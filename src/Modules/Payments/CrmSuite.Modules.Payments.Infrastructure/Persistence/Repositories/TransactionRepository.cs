using CrmSuite.BuildingBlocks.Infrastructure.Persistence;
using CrmSuite.Modules.Payments.Application.Abstractions;
using CrmSuite.Modules.Payments.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace CrmSuite.Modules.Payments.Infrastructure.Persistence.Repositories;

public sealed class TransactionRepository : ITransactionRepository
{
    private readonly AppDbContext _dbContext;

    public TransactionRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<Transaction?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        _dbContext.Set<Transaction>().SingleOrDefaultAsync(transaction => transaction.Id == id, cancellationToken);

    public Task<Transaction?> GetByTrackingCodeAsync(
        string trackingCode,
        CancellationToken cancellationToken)
    {
        var normalized = trackingCode.Trim().ToUpperInvariant();

        return _dbContext.Set<Transaction>()
            .SingleOrDefaultAsync(transaction => transaction.TrackingCode == normalized, cancellationToken);
    }

    public void Add(Transaction transaction) => _dbContext.Set<Transaction>().Add(transaction);
}
