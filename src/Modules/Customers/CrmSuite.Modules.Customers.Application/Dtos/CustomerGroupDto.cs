namespace CrmSuite.Modules.Customers.Application.Dtos;

public sealed record CustomerGroupDto(
    Guid Id,
    string Name,
    string? Description,
    DateTime CreatedAt,
    string CreatedAtJalali);
