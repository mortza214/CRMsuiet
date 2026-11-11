namespace CrmSuite.Modules.Identity.Application.Dtos;

public sealed record DepartmentDto(
    Guid Id,
    string Name,
    Guid? ParentId,
    DateTime CreatedAt,
    string CreatedAtJalali);
