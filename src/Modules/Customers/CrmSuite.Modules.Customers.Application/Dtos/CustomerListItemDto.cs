namespace CrmSuite.Modules.Customers.Application.Dtos;

public sealed record CustomerListItemDto(
    Guid Id,
    string Type,
    string DisplayName,
    string? FirstName,
    string? LastName,
    string? CompanyName,
    string Mobile,
    string NationalId,
    string? GroupName,
    bool IsReseller,
    Guid? ParentCustomerId,
    Guid? OwnerUserId,
    string? OwnerName,
    IReadOnlyList<CustomerTagSummaryDto> Tags,
    DateTime CreatedAt,
    string CreatedAtJalali);
