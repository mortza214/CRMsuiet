namespace CrmSuite.Modules.Forms.Domain.Enums;

public enum FormEntityType
{
    None = 0,
    Customer = 1,
    Order = 2,
    Service = 3,
    DataCenter = 4,
    Requirement = 5,
    Feasibility = 6,
    Ticket = 7,
}

public enum FormDefinitionStatus
{
    Draft = 1,
    Published = 2,
    Archived = 3,
}

public enum FormFieldType
{
    Text = 1,
    TextArea = 2,
    Number = 3,
    Select = 4,
    MultiSelect = 5,
    Checkbox = 6,
    Radio = 7,
    Date = 8,
    Email = 9,
    Phone = 10,
    File = 11,
}

public enum FormSubmissionStatus
{
    Draft = 1,
    Submitted = 2,
    Approved = 3,
    Rejected = 4,
}
