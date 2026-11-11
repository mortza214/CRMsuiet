using CrmSuite.Modules.Foundation.Domain.Enums;

namespace CrmSuite.Modules.Foundation.Application.Auditing;

public sealed record AuditEntry(
    string EntityName,
    string EntityId,
    AuditAction Action,
    object? OldValues,
    object? NewValues,
    Guid? UserId = null);
