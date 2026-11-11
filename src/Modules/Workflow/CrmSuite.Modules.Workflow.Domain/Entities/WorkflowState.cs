using CrmSuite.BuildingBlocks.Domain.Exceptions;
using CrmSuite.Modules.Foundation.Domain.Entities;
using CrmSuite.Modules.Workflow.Domain.Enums;

namespace CrmSuite.Modules.Workflow.Domain.Entities;

public sealed class WorkflowState : BaseEntity
{
    private WorkflowState()
    {
    }

    internal WorkflowState(
        Guid definitionId,
        string code,
        string name,
        WorkflowStateType stateType,
        int order,
        bool isFinal,
        WorkflowAssigneeType assigneeType,
        string? assigneeValue,
        string? formCode,
        string color,
        WorkflowActionKey actionKey = WorkflowActionKey.None)
        : base(Guid.NewGuid())
    {
        DefinitionId = definitionId;
        SetDetails(
            code,
            name,
            stateType,
            order,
            isFinal,
            assigneeType,
            assigneeValue,
            formCode,
            color,
            actionKey);
    }

    public Guid DefinitionId { get; private set; }

    public string Code { get; private set; } = string.Empty;

    public string Name { get; private set; } = string.Empty;

    public WorkflowStateType StateType { get; private set; }

    public int Order { get; private set; }

    public bool IsFinal { get; private set; }

    public WorkflowAssigneeType AssigneeType { get; private set; }

    public string? AssigneeValue { get; private set; }

    public string? FormCode { get; private set; }

    public WorkflowActionKey ActionKey { get; private set; } = WorkflowActionKey.None;

    public string Color { get; private set; } = "#1677ff";

    internal void SetDetails(
        string code,
        string name,
        WorkflowStateType stateType,
        int order,
        bool isFinal,
        WorkflowAssigneeType assigneeType,
        string? assigneeValue,
        string? formCode,
        string color,
        WorkflowActionKey actionKey = WorkflowActionKey.None)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            throw new DomainException("کد وضعیت الزامی است.", "WORKFLOW_STATE_CODE_REQUIRED");
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            throw new DomainException("نام وضعیت الزامی است.", "WORKFLOW_STATE_NAME_REQUIRED");
        }

        if (order < 0)
        {
            throw new DomainException("ترتیب وضعیت معتبر نیست.", "WORKFLOW_STATE_ORDER_INVALID");
        }

        if (!Enum.IsDefined(stateType))
        {
            throw new DomainException("نوع وضعیت معتبر نیست.", "WORKFLOW_STATE_TYPE_INVALID");
        }

        if (!Enum.IsDefined(assigneeType))
        {
            throw new DomainException("نوع مسئول وضعیت معتبر نیست.", "WORKFLOW_STATE_ASSIGNEE_INVALID");
        }

        if (!Enum.IsDefined(actionKey))
        {
            throw new DomainException("ActionKey is invalid.", "WORKFLOW_STATE_ACTION_INVALID");
        }

        if (actionKey == WorkflowActionKey.FillForm && string.IsNullOrWhiteSpace(formCode))
        {
            throw new DomainException(
                "FormCode is required when ActionKey is FillForm.",
                "WORKFLOW_STATE_FORM_REQUIRED");
        }

        Code = code.Trim().ToUpperInvariant();
        Name = name.Trim();
        StateType = stateType;
        Order = order;
        IsFinal = stateType == WorkflowStateType.End || isFinal;
        AssigneeType = assigneeType;
        AssigneeValue = string.IsNullOrWhiteSpace(assigneeValue) ? null : assigneeValue.Trim();
        FormCode = string.IsNullOrWhiteSpace(formCode) ? null : formCode.Trim();
        ActionKey = actionKey;
        Color = string.IsNullOrWhiteSpace(color) ? "#1677ff" : color.Trim();
    }
}
