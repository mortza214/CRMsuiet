using CrmSuite.BuildingBlocks.Domain.Results;
using CrmSuite.Modules.Identity.Application.Dtos;
using MediatR;

namespace CrmSuite.Modules.Identity.Application.Departments.Queries;

public sealed record GetDepartmentsQuery : IRequest<Result<IReadOnlyList<DepartmentDto>>>;

public sealed record GetDepartmentByIdQuery(Guid Id) : IRequest<Result<DepartmentDto>>;
