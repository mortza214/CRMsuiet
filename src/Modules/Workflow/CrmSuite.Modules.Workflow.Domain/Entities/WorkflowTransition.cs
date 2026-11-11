using CrmSuite.BuildingBlocks.Domain.Exceptions;
using CrmSuite.Modules.Foundation.Domain.Entities;
using CrmSuite.Modules.Workflow.Domain.Enums;

namespace CrmSuite.Modules.Workflow.Domain.Entities;

public sealed class WorkflowTransition : BaseEntity
{
    private WorkflowTransition()
    {
    }

    internal WorkflowTransition(
        Guid definitionId,
        Guid fromStateId,
        Guid toStateId,
        string name,
        WorkflowGuardType guardType,
        string? requiredPermission,
        string? guardPluginKey,
        bool requiresComment,
        int sortOrder)
        : base(Guid.NewGuid())
    {
        DefinitionId = definitionId;
        FromStateId = fromStateId;
        ToStateId = toStateId;
        SetDetails(
            name,
            guardType,
            requiredPermission,
            guardPluginKey,
            requiresComment,
            sortOrder);
    }

    public Guid DefinitionId { get; private set; }

    public Guid FromStateId { get; private set; }

    public Guid ToStateId { get; private set; }

    public string Name { get; private set; } = string.Empty;

    public WorkflowGuardType GuardType { get; private set; }

    public string? RequiredPermission { get; private set; }

    public string? GuardPluginKey { get; private set; }

    public bool RequiresComment { get; private set; }

    public int SortOrder { get; private set; }

    internal void SetDetails(
        string name,
        WorkflowGuardType guardType,
        string? requiredPermission,
        string? guardPluginKey,
        bool requiresComment,
        int sortOrder)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new DomainException("نام گذار الزامی است.", "WORKFLOW_TRANSITION_NAME_REQUIRED");
        }

        if (FromStateId == ToStateId)
        {
            throw new DomainException("وضعیت مبدأ و مقصد گذار نباید یکسان باشد.", "WORKFLOW_TRANSITION_SELF_LOOP");
        }

        if (guardType == WorkflowGuardType.Plugin && string.IsNullOrWhiteSpace(guardPluginKey))
        {
            throw new DomainException("کلید Plugin برای Guard الزامی است.", "WORKFLOW_GUARD_KEY_REQUIRED");
        }

        Name = name.Trim();
        GuardType = guardType;
        RequiredPermission = string.IsNullOrWhiteSpace(requiredPermission)
            ? null
            : requiredPermission.Trim();
        GuardPluginKey = string.IsNullOrWhiteSpace(guardPluginKey)
            ? null
            : guardPluginKey.Trim();
        RequiresComment = requiresComment;
        SortOrder = sortOrder;
    }
}
