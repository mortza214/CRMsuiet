namespace CrmSuite.Modules.Customers.Application.Dtos;

public sealed record CustomerTagDto(
    Guid Id,
    string Name,
    string Color,
    DateTime CreatedAt,
    string CreatedAtJalali);

public sealed record CustomerTagSummaryDto(Guid Id, string Name, string Color);
