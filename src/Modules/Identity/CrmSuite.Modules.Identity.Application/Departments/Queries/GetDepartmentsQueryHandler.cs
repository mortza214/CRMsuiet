using CrmSuite.BuildingBlocks.Domain.Results;
using CrmSuite.Modules.Identity.Application.Abstractions;
using CrmSuite.Modules.Identity.Application.Dtos;
using CrmSuite.Modules.Identity.Application.Mapping;
using MediatR;

namespace CrmSuite.Modules.Identity.Application.Departments.Queries;

public sealed class GetDepartmentsQueryHandler
    : IRequestHandler<GetDepartmentsQuery, Result<IReadOnlyList<DepartmentDto>>>
{
    private readonly IDepartmentRepository _departments;
    private readonly IdentityDtoMapper _mapper;

    public GetDepartmentsQueryHandler(IDepartmentRepository departments, IdentityDtoMapper mapper)
    {
        _departments = departments;
        _mapper = mapper;
    }

    public async Task<Result<IReadOnlyList<DepartmentDto>>> Handle(
        GetDepartmentsQuery request,
        CancellationToken cancellationToken)
    {
        var departments = await _departments.GetAllAsync(cancellationToken);
        return Result.Success<IReadOnlyList<DepartmentDto>>(
            departments.Select(_mapper.ToDepartmentDto).ToArray());
    }
}

public sealed class GetDepartmentByIdQueryHandler
    : IRequestHandler<GetDepartmentByIdQuery, Result<DepartmentDto>>
{
    private readonly IDepartmentRepository _departments;
    private readonly IdentityDtoMapper _mapper;

    public GetDepartmentByIdQueryHandler(IDepartmentRepository departments, IdentityDtoMapper mapper)
    {
        _departments = departments;
        _mapper = mapper;
    }

    public async Task<Result<DepartmentDto>> Handle(
        GetDepartmentByIdQuery request,
        CancellationToken cancellationToken)
    {
        var department = await _departments.GetByIdAsync(request.Id, cancellationToken);

        return department is null
            ? Result.Failure<DepartmentDto>("دپارتمان یافت نشد.", "IDENTITY_DEPARTMENT_NOT_FOUND")
            : Result.Success(_mapper.ToDepartmentDto(department));
    }
}
