using CrmSuite.BuildingBlocks.Domain.Results;
using MediatR;

namespace CrmSuite.Modules.Identity.Application.Departments.Commands;

public sealed record DeleteDepartmentCommand(Guid Id) : IRequest<Result>;
