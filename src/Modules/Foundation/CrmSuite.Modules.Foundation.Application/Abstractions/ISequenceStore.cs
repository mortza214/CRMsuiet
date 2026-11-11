using CrmSuite.Modules.Foundation.Application.Sequences;

namespace CrmSuite.Modules.Foundation.Application.Abstractions;

public interface ISequenceStore
{
    /// <summary>Atomically increments the sequence and returns the allocated state.</summary>
    Task<SequenceAllocation> AllocateNextAsync(string sequenceName, CancellationToken cancellationToken);
}
