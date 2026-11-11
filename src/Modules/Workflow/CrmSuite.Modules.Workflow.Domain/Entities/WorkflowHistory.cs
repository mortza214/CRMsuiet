using CrmSuite.Modules.Foundation.Domain.Entities;

namespace CrmSuite.Modules.Workflow.Domain.Entities;

public sealed class WorkflowHistory : BaseEntity
{
    private WorkflowHistory()
    {
    }

    public WorkflowHistory(
        Guid instanceId,
        Guid? fromStateId,
        Guid toStateId,
        Guid? transitionId,
        Guid? actionBy,
        DateTime actionAtUtc,
        string? comment,
        string? payloadJson,
        string? idempotencyKey)
        : base(Guid.NewGuid())
    {
        InstanceId = instanceId;
        FromStateId = fromStateId;
        ToStateId = toStateId;
        TransitionId = transitionId;
        ActionBy = actionBy;
        ActionAt = DateTime.SpecifyKind(actionAtUtc, DateTimeKind.Utc);
        Comment = string.IsNullOrWhiteSpace(comment) ? null : comment.Trim();
        PayloadJson = string.IsNullOrWhiteSpace(payloadJson) ? null : payloadJson;
        IdempotencyKey = string.IsNullOrWhiteSpace(idempotencyKey) ? null : idempotencyKey.Trim();
        MarkCreated(actionBy, actionAtUtc);
    }

    public Guid InstanceId { get; private set; }

    public Guid? FromStateId { get; private set; }

    public Guid ToStateId { get; private set; }

    public Guid? TransitionId { get; private set; }

    public Guid? ActionBy { get; private set; }

    public DateTime ActionAt { get; private set; }

    public string? Comment { get; private set; }

    public string? PayloadJson { get; private set; }

    public string? IdempotencyKey { get; private set; }
}
