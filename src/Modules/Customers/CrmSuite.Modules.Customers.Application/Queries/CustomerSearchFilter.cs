namespace CrmSuite.Modules.Customers.Application.Queries;

public sealed record CustomerSearchFilter(
    string? Search,
    Guid? GroupId,
    Guid? TagId,
    bool? IsReseller,
    bool OnlyDebtors,
    Guid? ParentCustomerId,
    int Page,
    int PageSize);
