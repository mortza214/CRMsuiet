using CrmSuite.BuildingBlocks.Domain.Exceptions;
using CrmSuite.Modules.Foundation.Domain.Entities;
using CrmSuite.Modules.Workflow.Domain.Enums;

namespace CrmSuite.Modules.Workflow.Domain.Entities;

public sealed class WorkflowTask : BaseEntity
{
    private WorkflowTask()
    {
    }

    public WorkflowTask(
        Guid instanceId,
        Guid stateId,
        Guid? assignedUserId,
        Guid? assignedRoleId,
        Guid? assignedDepartmentId,
        DateTime? dueDate,
        Guid? createdBy,
        DateTime createdAtUtc)
        : base(Guid.NewGuid())
    {
        InstanceId = instanceId;
        StateId = stateId;
        AssignedUserId = assignedUserId;
        AssignedRoleId = assignedRoleId;
        AssignedDepartmentId = assignedDepartmentId;
        DueDate = dueDate.HasValue
            ? DateTime.SpecifyKind(dueDate.Value, DateTimeKind.Utc)
            : null;
        Status = WorkflowTaskStatus.Pending;
        MarkCreated(createdBy, createdAtUtc);
    }

    public Guid InstanceId { get; private set; }

    public Guid StateId { get; private set; }

    public Guid? AssignedUserId { get; private set; }

    public Guid? AssignedRoleId { get; private set; }

    public Guid? AssignedDepartmentId { get; private set; }

    public WorkflowTaskStatus Status { get; private set; }

    public DateTime? DueDate { get; private set; }

    public Guid? CompletedBy { get; private set; }

    public DateTime? CompletedAt { get; private set; }

    public void Reassign(
        Guid? userId,
        Guid? roleId,
        Guid? departmentId,
        Guid? actorId,
        DateTime utcNow)
    {
        if (Status is WorkflowTaskStatus.Done or WorkflowTaskStatus.Cancelled)
        {
            throw new DomainException("تسک خاتمه‌یافته قابل ارجاع مجدد نیست.", "WORKFLOW_TASK_CLOSED");
        }

        AssignedUserId = userId;
        AssignedRoleId = roleId;
        AssignedDepartmentId = departmentId;
        Status = WorkflowTaskStatus.Pending;
        MarkUpdated(actorId, utcNow);
    }

    public void Start(Guid? userId, DateTime utcNow)
    {
        if (Status != WorkflowTaskStatus.Pending)
        {
            return;
        }

        Status = WorkflowTaskStatus.InProgress;
        MarkUpdated(userId, utcNow);
    }

    public void Complete(Guid? userId, DateTime utcNow)
    {
        if (Status is WorkflowTaskStatus.Done or WorkflowTaskStatus.Cancelled)
        {
            return;
        }

        Status = WorkflowTaskStatus.Done;
        CompletedBy = userId;
        CompletedAt = DateTime.SpecifyKind(utcNow, DateTimeKind.Utc);
        MarkUpdated(userId, utcNow);
    }

    public void Cancel(Guid? userId, DateTime utcNow)
    {
        if (Status == WorkflowTaskStatus.Done)
        {
            return;
        }

        Status = WorkflowTaskStatus.Cancelled;
        MarkUpdated(userId, utcNow);
    }
}
