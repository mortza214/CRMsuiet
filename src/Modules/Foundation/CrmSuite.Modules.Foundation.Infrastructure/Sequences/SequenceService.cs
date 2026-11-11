using CrmSuite.BuildingBlocks.Application.Abstractions;
using CrmSuite.BuildingBlocks.Domain.Exceptions;
using CrmSuite.Modules.Foundation.Application.Abstractions;
using CrmSuite.Modules.Foundation.Domain.Sequences;

namespace CrmSuite.Modules.Foundation.Infrastructure.Sequences;

public sealed class SequenceService : ISequenceService
{
    private readonly ISequenceStore _store;
    private readonly IJalaliDateService _jalaliDate;
    private readonly IDateTimeProvider _clock;

    public SequenceService(
        ISequenceStore store,
        IJalaliDateService jalaliDate,
        IDateTimeProvider clock)
    {
        _store = store;
        _jalaliDate = jalaliDate;
        _clock = clock;
    }

    public async Task<string> GetNextValueAsync(
        string sequenceName,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(sequenceName))
        {
            throw new DomainException("نام Sequence الزامی است.", "FOUNDATION_SEQUENCE_NAME_REQUIRED");
        }

        var allocation = await _store.AllocateNextAsync(sequenceName.Trim(), cancellationToken);
        var now = _clock.UtcNow;

        return SequenceNumberFormatter.Format(
            allocation.Prefix,
            allocation.Format,
            allocation.CurrentValue,
            _jalaliDate.GetJalaliYear(now),
            now.Year);
    }
}
