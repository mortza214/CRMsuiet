using CrmSuite.BuildingBlocks.Domain.Entities;
using CrmSuite.Modules.Foundation.Domain.Enums;

namespace CrmSuite.Modules.Foundation.Domain.Entities;

/// <summary>
/// Append-only audit record. Old/New values are sanitized JSON produced by the audit service.
/// </summary>
public sealed class AuditLog : Entity<Guid>
{
    private AuditLog()
    {
    }

    private AuditLog(
        string entityName,
        string entityId,
        AuditAction action,
        string? oldValues,
        string? newValues,
        Guid? userId,
        string? userIp,
        DateTime timestamp)
    {
        Id = Guid.NewGuid();
        EntityName = entityName;
        EntityId = entityId;
        Action = action;
        OldValues = oldValues;
        NewValues = newValues;
        UserId = userId;
        UserIp = userIp;
        Timestamp = timestamp;
    }

    public string EntityName { get; private set; } = string.Empty;

    public string EntityId { get; private set; } = string.Empty;

    public AuditAction Action { get; private set; }

    public string? OldValues { get; private set; }

    public string? NewValues { get; private set; }

    public Guid? UserId { get; private set; }

    public string? UserIp { get; private set; }

    public DateTime Timestamp { get; private set; }

    public static AuditLog Create(
        string entityName,
        string entityId,
        AuditAction action,
        string? oldValues,
        string? newValues,
        Guid? userId,
        string? userIp,
        DateTime timestamp)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(entityName);
        ArgumentException.ThrowIfNullOrWhiteSpace(entityId);

        return new AuditLog(
            entityName.Trim(),
            entityId.Trim(),
            action,
            oldValues,
            newValues,
            userId,
            string.IsNullOrWhiteSpace(userIp) ? null : userIp.Trim(),
            timestamp);
    }
}
