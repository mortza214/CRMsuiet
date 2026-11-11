using CrmSuite.BuildingBlocks.Domain.Results;
using CrmSuite.Modules.Identity.Application.Abstractions;
using CrmSuite.Modules.Identity.Application.Dtos;
using CrmSuite.Modules.Identity.Application.Mapping;
using MediatR;

namespace CrmSuite.Modules.Identity.Application.Permissions.Queries;

public sealed class GetPermissionsQueryHandler
    : IRequestHandler<GetPermissionsQuery, Result<IReadOnlyList<PermissionGroupDto>>>
{
    private readonly IPermissionRepository _permissions;
    private readonly IdentityDtoMapper _mapper;

    public GetPermissionsQueryHandler(IPermissionRepository permissions, IdentityDtoMapper mapper)
    {
        _permissions = permissions;
        _mapper = mapper;
    }

    public async Task<Result<IReadOnlyList<PermissionGroupDto>>> Handle(
        GetPermissionsQuery request,
        CancellationToken cancellationToken)
    {
        var permissions = await _permissions.GetAllAsync(cancellationToken);

        var groups = permissions
            .GroupBy(permission => permission.Group, StringComparer.Ordinal)
            .OrderBy(group => group.Key, StringComparer.Ordinal)
            .Select(group => new PermissionGroupDto(
                group.Key,
                group.OrderBy(permission => permission.Code, StringComparer.Ordinal)
                    .Select(_mapper.ToPermissionDto)
                    .ToArray()))
            .ToArray();

        return Result.Success<IReadOnlyList<PermissionGroupDto>>(groups);
    }
}
