namespace CrmSuite.Modules.Foundation.Application.Abstractions;

public interface ISequenceService
{
    Task<string> GetNextValueAsync(string sequenceName, CancellationToken cancellationToken = default);
}
