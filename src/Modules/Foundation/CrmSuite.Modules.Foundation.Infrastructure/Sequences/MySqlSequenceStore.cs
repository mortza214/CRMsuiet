using System.Data;
using CrmSuite.BuildingBlocks.Application.Abstractions;
using CrmSuite.BuildingBlocks.Domain.Exceptions;
using CrmSuite.BuildingBlocks.Infrastructure.Persistence;
using CrmSuite.Modules.Foundation.Application.Abstractions;
using CrmSuite.Modules.Foundation.Application.Sequences;
using CrmSuite.Modules.Foundation.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using FoundationCurrentUser = CrmSuite.Modules.Foundation.Application.Abstractions.ICurrentUserService;

namespace CrmSuite.Modules.Foundation.Infrastructure.Sequences;

/// <summary>
/// Uses SELECT ... FOR UPDATE inside an explicit transaction. The row lock is held until the
/// commit, so concurrent callers cannot observe or write the same CurrentValue.
/// </summary>
public sealed class MySqlSequenceStore : ISequenceStore
{
    private readonly AppDbContext _dbContext;
    private readonly FoundationCurrentUser _currentUser;
    private readonly IDateTimeProvider _clock;

    public MySqlSequenceStore(
        AppDbContext dbContext,
        FoundationCurrentUser currentUser,
        IDateTimeProvider clock)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
        _clock = clock;
    }

    public async Task<SequenceAllocation> AllocateNextAsync(
        string sequenceName,
        CancellationToken cancellationToken)
    {
        var strategy = _dbContext.Database.CreateExecutionStrategy();

        return await strategy.ExecuteAsync(async () =>
        {
            await using var transaction = await _dbContext.Database.BeginTransactionAsync(
                IsolationLevel.ReadCommitted,
                cancellationToken);

            var sequence = await _dbContext.Set<Sequence>()
                .FromSqlInterpolated(
                    $"SELECT * FROM sequences WHERE name = {sequenceName} AND is_deleted = 0 FOR UPDATE")
                .SingleOrDefaultAsync(cancellationToken);

            if (sequence is null)
            {
                throw new DomainException(
                    $"Sequence با نام «{sequenceName}» تعریف نشده است.",
                    "FOUNDATION_SEQUENCE_NOT_FOUND");
            }

            sequence.AllocateNext();
            sequence.MarkUpdated(_currentUser.UserId, _clock.UtcNow);
            await _dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            return new SequenceAllocation(sequence.Prefix, sequence.CurrentValue, sequence.Format);
        });
    }
}
