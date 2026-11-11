using CrmSuite.BuildingBlocks.Domain.Exceptions;
using CrmSuite.Modules.Foundation.Domain.Entities;
using CrmSuite.Modules.Workflow.Domain.Enums;

namespace CrmSuite.Modules.Workflow.Domain.Entities;

public sealed class WorkflowDefinition : BaseEntity
{
    private WorkflowDefinition()
    {
    }

    public WorkflowDefinition(
        string code,
        string name,
        WorkflowEntityType entityType,
        string? description,
        Guid? createdByUserId,
        DateTime createdAtUtc)
        : base(Guid.NewGuid())
    {
        Code = NormalizeCode(code);
        Name = NormalizeName(name);
        EntityType = entityType;
        Description = NormalizeDescription(description);
        Version = 1;
        Status = WorkflowDefinitionStatus.Draft;
        MarkCreated(createdByUserId, createdAtUtc);
    }

    public string Code { get; private set; } = string.Empty;

    public string Name { get; private set; } = string.Empty;

    public WorkflowEntityType EntityType { get; private set; }

    public int Version { get; private set; }

    public WorkflowDefinitionStatus Status { get; private set; }

    public string? Description { get; private set; }

    public DateTime? PublishedAt { get; private set; }

    public ICollection<WorkflowState> States { get; private set; } = new List<WorkflowState>();

    public ICollection<WorkflowTransition> Transitions { get; private set; } = new List<WorkflowTransition>();

    public void UpdateDraft(
        string name,
        string? description,
        Guid? userId,
        DateTime utcNow)
    {
        EnsureDraft("ویرایش تعریف");
        Name = NormalizeName(name);
        Description = NormalizeDescription(description);
        MarkUpdated(userId, utcNow);
    }

    public WorkflowState AddState(
        string code,
        string name,
        WorkflowStateType stateType,
        int order,
        bool isFinal,
        WorkflowAssigneeType assigneeType,
        string? assigneeValue,
        string? formCode,
        string color,
        WorkflowActionKey actionKey = WorkflowActionKey.None,
        double x = 0,
        double y = 0)
    {
        EnsureDraft("افزودن وضعیت");
        var normalizedCode = NormalizeStateCode(code);

        if (States.Any(state => string.Equals(state.Code, normalizedCode, StringComparison.OrdinalIgnoreCase)))
        {
            throw new DomainException("کد وضعیت تکراری است.", "WORKFLOW_STATE_CODE_DUPLICATE");
        }

        if (stateType == WorkflowStateType.Start && States.Any(state => state.StateType == WorkflowStateType.Start))
        {
            throw new DomainException("هر تعریف فقط یک وضعیت شروع می‌تواند داشته باشد.", "WORKFLOW_START_DUPLICATE");
        }

        var state = new WorkflowState(
            Id,
            normalizedCode,
            name,
            stateType,
            order,
            isFinal,
            assigneeType,
            assigneeValue,
            formCode,
            color,
            actionKey,
            x,
            y);
        States.Add(state);
        MarkUpdated(null, DateTime.UtcNow);
        return state;
    }

    public void UpdateState(
        Guid stateId,
        string code,
        string name,
        WorkflowStateType stateType,
        int order,
        bool isFinal,
        WorkflowAssigneeType assigneeType,
        string? assigneeValue,
        string? formCode,
        string color,
        Guid? userId,
        DateTime utcNow,
        WorkflowActionKey actionKey = WorkflowActionKey.None,
        double x = 0,
        double y = 0)
    {
        EnsureDraft("ویرایش وضعیت");
        var state = FindState(stateId);
        var normalizedCode = NormalizeStateCode(code);

        if (States.Any(other =>
                other.Id != stateId
                && string.Equals(other.Code, normalizedCode, StringComparison.OrdinalIgnoreCase)))
        {
            throw new DomainException("کد وضعیت تکراری است.", "WORKFLOW_STATE_CODE_DUPLICATE");
        }

        if (stateType == WorkflowStateType.Start
            && States.Any(other => other.Id != stateId && other.StateType == WorkflowStateType.Start))
        {
            throw new DomainException("هر تعریف فقط یک وضعیت شروع می‌تواند داشته باشد.", "WORKFLOW_START_DUPLICATE");
        }

        state.SetDetails(
            normalizedCode,
            name,
            stateType,
            order,
            isFinal,
            assigneeType,
            assigneeValue,
            formCode,
            color,
            actionKey,
            x,
            y);
        MarkUpdated(userId, utcNow);
    }

    public void RemoveState(Guid stateId, Guid? userId, DateTime utcNow)
    {
        EnsureDraft("حذف وضعیت");
        var state = FindState(stateId);

        foreach (var transition in Transitions
                     .Where(item => item.FromStateId == stateId || item.ToStateId == stateId)
                     .ToArray())
        {
            Transitions.Remove(transition);
        }

        States.Remove(state);
        MarkUpdated(userId, utcNow);
    }

    public WorkflowTransition AddTransition(
        Guid fromStateId,
        Guid toStateId,
        string name,
        WorkflowGuardType guardType,
        string? requiredPermission,
        string? guardPluginKey,
        bool requiresComment,
        int sortOrder,
        Guid? userId,
        DateTime utcNow)
    {
        EnsureDraft("افزودن گذار");
        _ = FindState(fromStateId);
        _ = FindState(toStateId);

        if (Transitions.Any(item =>
                item.FromStateId == fromStateId && item.ToStateId == toStateId))
        {
            throw new DomainException(
                "گذار تکراری برای این مبدأ و مقصد وجود دارد.",
                "WORKFLOW_TRANSITION_DUPLICATE");
        }

        var transition = new WorkflowTransition(
            Id,
            fromStateId,
            toStateId,
            name,
            guardType,
            requiredPermission,
            guardPluginKey,
            requiresComment,
            sortOrder);
        Transitions.Add(transition);
        MarkUpdated(userId, utcNow);
        return transition;
    }

    public void RemoveTransition(Guid transitionId, Guid? userId, DateTime utcNow)
    {
        EnsureDraft("حذف گذار");
        var transition = Transitions.SingleOrDefault(item => item.Id == transitionId)
            ?? throw new DomainException("گذار یافت نشد.", "WORKFLOW_TRANSITION_NOT_FOUND");

        Transitions.Remove(transition);
        MarkUpdated(userId, utcNow);
    }

    public void Publish(Guid? userId, DateTime utcNow)
    {
        if (Status != WorkflowDefinitionStatus.Draft)
        {
            throw new DomainException("فقط نسخه Draft قابل انتشار است.", "WORKFLOW_DEFINITION_NOT_DRAFT");
        }

        ValidateForPublish();
        Status = WorkflowDefinitionStatus.Published;
        PublishedAt = DateTime.SpecifyKind(utcNow, DateTimeKind.Utc);
        MarkUpdated(userId, utcNow);
    }

    /// <summary>
    /// Testing-only bridge: returns a published definition to Draft without creating a version.
    /// A subsequent Publish is required before new instances can start.
    /// </summary>
    public void ReopenForEditing(Guid? userId, DateTime utcNow)
    {
        if (Status != WorkflowDefinitionStatus.Published)
        {
            throw new DomainException(
                "فقط تعریف منتشرشده قابل بازگشت به ویرایش است.",
                "WORKFLOW_DEFINITION_NOT_PUBLISHED");
        }

        Status = WorkflowDefinitionStatus.Draft;
        MarkUpdated(userId, utcNow);
    }

    public void Archive(Guid? userId, DateTime utcNow)
    {
        if (Status != WorkflowDefinitionStatus.Published)
        {
            throw new DomainException("فقط تعریف منتشرشده قابل آرشیو است.", "WORKFLOW_DEFINITION_NOT_PUBLISHED");
        }

        Status = WorkflowDefinitionStatus.Archived;
        MarkUpdated(userId, utcNow);
    }

    public void ValidateForPublish()
    {
        if (States.Count == 0)
        {
            throw new DomainException("گردش کار باید حداقل یک وضعیت داشته باشد.", "WORKFLOW_STATES_EMPTY");
        }

        var starts = States.Where(state => state.StateType == WorkflowStateType.Start).ToArray();
        if (starts.Length != 1)
        {
            throw new DomainException(
                "گردش کار باید دقیقاً یک وضعیت شروع داشته باشد.",
                "WORKFLOW_START_REQUIRED");
        }

        if (!States.Any(state => state.StateType == WorkflowStateType.End))
        {
            throw new DomainException(
                "گردش کار باید حداقل یک وضعیت پایان داشته باشد.",
                "WORKFLOW_END_REQUIRED");
        }

        if (Transitions
            .GroupBy(item => new { item.FromStateId, item.ToStateId })
            .Any(group => group.Count() > 1))
        {
            throw new DomainException(
                "گذار تکراری در گردش کار وجود دارد.",
                "WORKFLOW_TRANSITION_DUPLICATE");
        }

        var stateIds = States.Select(state => state.Id).ToHashSet();
        if (Transitions.Any(item => !stateIds.Contains(item.FromStateId) || !stateIds.Contains(item.ToStateId)))
        {
            throw new DomainException(
                "گذار به وضعیت نامعتبر اشاره می‌کند.",
                "WORKFLOW_TRANSITION_STATE_INVALID");
        }

        var startId = starts[0].Id;
        var visited = new HashSet<Guid> { startId };
        var queue = new Queue<Guid>();
        queue.Enqueue(startId);

        while (queue.Count > 0)
        {
            var current = queue.Dequeue();
            foreach (var transition in Transitions.Where(item => item.FromStateId == current))
            {
                if (visited.Add(transition.ToStateId))
                {
                    queue.Enqueue(transition.ToStateId);
                }
            }
        }

        var unreachable = States
            .Where(state => !visited.Contains(state.Id))
            .Select(state => state.Code)
            .ToArray();

        if (unreachable.Length > 0)
        {
            throw new DomainException(
                $"وضعیت‌های بدون مسیر از Start: {string.Join(", ", unreachable)}",
                "WORKFLOW_STATE_ORPHAN");
        }
    }

    public WorkflowState FindState(Guid stateId) =>
        States.SingleOrDefault(state => state.Id == stateId)
        ?? throw new DomainException("وضعیت یافت نشد.", "WORKFLOW_STATE_NOT_FOUND");

    public WorkflowTransition FindTransition(Guid transitionId) =>
        Transitions.SingleOrDefault(transition => transition.Id == transitionId)
        ?? throw new DomainException("گذار یافت نشد.", "WORKFLOW_TRANSITION_NOT_FOUND");

    private void EnsureDraft(string action)
    {
        if (Status != WorkflowDefinitionStatus.Draft)
        {
            throw new DomainException(
                $"{action} فقط برای نسخه Draft مجاز است.",
                "WORKFLOW_DEFINITION_NOT_DRAFT");
        }
    }

    private static string NormalizeCode(string code)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            throw new DomainException("کد گردش کار الزامی است.", "WORKFLOW_DEFINITION_CODE_REQUIRED");
        }

        var normalized = code.Trim().ToUpperInvariant();
        if (normalized.Length > 80
            || !System.Text.RegularExpressions.Regex.IsMatch(normalized, "^[A-Z0-9._-]+$"))
        {
            throw new DomainException("کد گردش کار معتبر نیست.", "WORKFLOW_DEFINITION_CODE_INVALID");
        }

        return normalized;
    }

    private static string NormalizeName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new DomainException("نام گردش کار الزامی است.", "WORKFLOW_DEFINITION_NAME_REQUIRED");
        }

        return name.Trim().Length > 200 ? name.Trim()[..200] : name.Trim();
    }

    private static string NormalizeStateCode(string code)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            throw new DomainException("کد وضعیت الزامی است.", "WORKFLOW_STATE_CODE_REQUIRED");
        }

        return code.Trim().ToUpperInvariant();
    }

    private static string? NormalizeDescription(string? description) =>
        string.IsNullOrWhiteSpace(description) ? null : description.Trim();
}
