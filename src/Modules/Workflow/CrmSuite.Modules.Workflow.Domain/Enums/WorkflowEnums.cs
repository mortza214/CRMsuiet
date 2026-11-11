namespace CrmSuite.Modules.Workflow.Domain.Enums;

public enum WorkflowEntityType
{
    Order = 1,
    Ticket = 2,
    Customer = 3,
    ManualTask = 4,
}

public enum WorkflowDefinitionStatus
{
    Draft = 1,
    Published = 2,
    Archived = 3,
}

public enum WorkflowStateType
{
    Start = 1,
    Normal = 2,
    End = 3,
}

public enum WorkflowAssigneeType
{
    User = 1,
    Role = 2,
    Department = 3,
    CustomerOwner = 4,
    None = 5,
    Customer = 6,
}

public enum WorkflowGuardType
{
    None = 1,
    PermissionOnly = 2,
    Plugin = 3,
}

public enum WorkflowInstanceStatus
{
    Running = 1,
    Completed = 2,
    Cancelled = 3,
}

public enum WorkflowActionKey
{
    None = 1,
    FillForm = 2,
    IssueProforma = 3,
    RequestPayment = 4,
    IssueFinalInvoice = 5,
    ActivateService = 6,
    ReviewFeasibility = 7,
    DecideSiteVisit = 8,
    SelectEquipment = 9,
    PurchaseEquipment = 10,
    CustomerAction = 11,
    PayInvoice = 12,
    VerifyPayment = 13,
    SignContract = 14,
    ReviewInstallation = 15,
}

public enum WorkflowTaskStatus
{
    Pending = 1,
    InProgress = 2,
    Done = 3,
    Cancelled = 4,
}
