using CrmSuite.BuildingBlocks.Domain.Results;
using CrmSuite.Modules.Identity.Application.Abstractions;
using CrmSuite.Modules.Identity.Application.Dtos;
using CrmSuite.Modules.Identity.Application.Mapping;
using MediatR;

namespace CrmSuite.Modules.Identity.Application.Roles.Queries;

public sealed class GetRolesQueryHandler
    : IRequestHandler<GetRolesQuery, Result<IReadOnlyList<RoleDto>>>
{
    private readonly IRoleRepository _roles;
    private readonly IdentityDtoMapper _mapper;

    public GetRolesQueryHandler(IRoleRepository roles, IdentityDtoMapper mapper)
    {
        _roles = roles;
        _mapper = mapper;
    }

    public async Task<Result<IReadOnlyList<RoleDto>>> Handle(
        GetRolesQuery request,
        CancellationToken cancellationToken)
    {
        var roles = await _roles.GetAllAsync(includePermissions: true, cancellationToken);
        return Result.Success<IReadOnlyList<RoleDto>>(roles.Select(_mapper.ToRoleDto).ToArray());
    }
}

public sealed class GetRoleByIdQueryHandler : IRequestHandler<GetRoleByIdQuery, Result<RoleDto>>
{
    private readonly IRoleRepository _roles;
    private readonly IdentityDtoMapper _mapper;

    public GetRoleByIdQueryHandler(IRoleRepository roles, IdentityDtoMapper mapper)
    {
        _roles = roles;
        _mapper = mapper;
    }

    public async Task<Result<RoleDto>> Handle(
        GetRoleByIdQuery request,
        CancellationToken cancellationToken)
    {
        var role = await _roles.GetByIdWithPermissionsAsync(request.Id, cancellationToken);

        return role is null
            ? Result.Failure<RoleDto>("نقش یافت نشد.", "IDENTITY_ROLE_NOT_FOUND")
            : Result.Success(_mapper.ToRoleDto(role));
    }
}
