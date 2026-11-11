using CrmSuite.BuildingBlocks.Domain.Results;
using CrmSuite.Modules.Identity.Application.Dtos;
using MediatR;

namespace CrmSuite.Modules.Identity.Application.Departments.Commands;

public sealed record UpdateDepartmentCommand(
    Guid Id,
    string Name,
    Guid? ParentId = null) : IRequest<Result<DepartmentDto>>;
