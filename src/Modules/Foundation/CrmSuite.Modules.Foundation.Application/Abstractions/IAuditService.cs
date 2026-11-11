using CrmSuite.Modules.Foundation.Application.Auditing;

namespace CrmSuite.Modules.Foundation.Application.Abstractions;

public interface IAuditService
{
    Task RecordAsync(AuditEntry entry, CancellationToken cancellationToken = default);
}
