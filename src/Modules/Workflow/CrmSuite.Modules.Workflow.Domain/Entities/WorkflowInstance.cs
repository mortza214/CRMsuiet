using CrmSuite.BuildingBlocks.Domain.Exceptions;
using CrmSuite.Modules.Foundation.Domain.Entities;
using CrmSuite.Modules.Workflow.Domain.Enums;

namespace CrmSuite.Modules.Workflow.Domain.Entities;

public sealed class WorkflowInstance : BaseEntity
{
    private WorkflowInstance()
    {
    }

    public WorkflowInstance(
        WorkflowDefinition definition,
        WorkflowEntityType entityType,
        Guid entityId,
        Guid currentStateId,
        Guid? startedBy,
        DateTime startedAtUtc,
        string? correlationId = null,
        string? idempotencyKey = null)
        : base(Guid.NewGuid())
    {
        DefinitionId = definition.Id;
        DefinitionVersion = definition.Version;
        EntityType = entityType;
        EntityId = entityId;
        CurrentStateId = currentStateId;
        Status = WorkflowInstanceStatus.Running;
        StartedBy = startedBy;
        StartedAt = DateTime.SpecifyKind(startedAtUtc, DateTimeKind.Utc);
        CorrelationId = string.IsNullOrWhiteSpace(correlationId) ? null : correlationId.Trim();
        IdempotencyKey = string.IsNullOrWhiteSpace(idempotencyKey) ? null : idempotencyKey.Trim();
        MarkCreated(startedBy, startedAtUtc);
    }

    public Guid DefinitionId { get; private set; }

    public int DefinitionVersion { get; private set; }

    public WorkflowEntityType EntityType { get; private set; }

    public Guid EntityId { get; private set; }

    public Guid CurrentStateId { get; private set; }

    public WorkflowInstanceStatus Status { get; private set; }

    public Guid? StartedBy { get; private set; }

    public DateTime StartedAt { get; private set; }

    public DateTime? CompletedAt { get; private set; }

    public string? CorrelationId { get; private set; }

    public string? IdempotencyKey { get; private set; }

    public ICollection<WorkflowTask> Tasks { get; private set; } = new List<WorkflowTask>();

    public ICollection<WorkflowHistory> History { get; private set; } = new List<WorkflowHistory>();

    public WorkflowHistory AddStartHistory(
        Guid stateId,
        Guid? actionBy,
        DateTime actionAtUtc,
        string? comment,
        string? payloadJson)
    {
        var history = new WorkflowHistory(
            Id,
            null,
            stateId,
            null,
            actionBy,
            actionAtUtc,
            comment,
            payloadJson,
            null);
        History.Add(history);
        return history;
    }

    public WorkflowHistory ExecuteTransition(
        WorkflowTransition transition,
        WorkflowState targetState,
        Guid? actionBy,
        DateTime actionAtUtc,
        string? comment,
        string? payloadJson,
        string? idempotencyKey,
        WorkflowTask? currentTask,
        WorkflowTask? nextTask)
    {
        if (Status != WorkflowInstanceStatus.Running)
        {
            throw new DomainException("گردش کار در وضعیت اجرا نیست.", "WORKFLOW_INSTANCE_NOT_RUNNING");
        }

        if (transition.FromStateId != CurrentStateId)
        {
            throw new DomainException(
                "گذار انتخاب‌شده از وضعیت فعلی قابل اجرا نیست.",
                "WORKFLOW_TRANSITION_NOT_ALLOWED");
        }

        if (transition.ToStateId != targetState.Id)
        {
            throw new DomainException("وضعیت مقصد گذار معتبر نیست.", "WORKFLOW_TRANSITION_TARGET_INVALID");
        }

        currentTask?.Complete(actionBy, actionAtUtc);

        var history = new WorkflowHistory(
            Id,
            CurrentStateId,
            targetState.Id,
            transition.Id,
            actionBy,
            actionAtUtc,
            comment,
            payloadJson,
            idempotencyKey);
        History.Add(history);

        CurrentStateId = targetState.Id;

        if (nextTask is not null)
        {
            Tasks.Add(nextTask);
        }

        if (targetState.IsFinal || targetState.StateType == WorkflowStateType.End)
        {
            Status = WorkflowInstanceStatus.Completed;
            CompletedAt = DateTime.SpecifyKind(actionAtUtc, DateTimeKind.Utc);
        }

        MarkUpdated(actionBy, actionAtUtc);
        return history;
    }

    public void Cancel(Guid? actionBy, DateTime utcNow, string? reason)
    {
        if (Status != WorkflowInstanceStatus.Running)
        {
            throw new DomainException("گردش کار قابل لغو نیست.", "WORKFLOW_INSTANCE_NOT_RUNNING");
        }

        foreach (var task in Tasks.Where(task =>
                     task.Status is WorkflowTaskStatus.Pending or WorkflowTaskStatus.InProgress))
        {
            task.Cancel(actionBy, utcNow);
        }

        History.Add(new WorkflowHistory(
            Id,
            CurrentStateId,
            CurrentStateId,
            null,
            actionBy,
            utcNow,
            reason,
            null,
            null));

        Status = WorkflowInstanceStatus.Cancelled;
        CompletedAt = DateTime.SpecifyKind(utcNow, DateTimeKind.Utc);
        MarkUpdated(actionBy, utcNow);
    }

    public void AddTask(WorkflowTask task)
    {
        if (task.InstanceId != Id)
        {
            throw new DomainException("تسک به این گردش کار تعلق ندارد.", "WORKFLOW_TASK_INSTANCE_MISMATCH");
        }

        Tasks.Add(task);
    }
}
