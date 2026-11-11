using CrmSuite.BuildingBlocks.Domain.Results;
using CrmSuite.Modules.Identity.Application.Abstractions;
using CrmSuite.Modules.Identity.Application.Dtos;
using CrmSuite.Modules.Identity.Application.Mapping;
using MediatR;
using FoundationCurrentUser = CrmSuite.Modules.Foundation.Application.Abstractions.ICurrentUserService;

namespace CrmSuite.Modules.Identity.Application.Users.Queries;

public sealed class GetCurrentUserQueryHandler
    : IRequestHandler<GetCurrentUserQuery, Result<UserProfileDto>>
{
    private readonly IUserRepository _users;
    private readonly IPermissionProvider _permissionProvider;
    private readonly FoundationCurrentUser _currentUser;
    private readonly IdentityDtoMapper _mapper;

    public GetCurrentUserQueryHandler(
        IUserRepository users,
        IPermissionProvider permissionProvider,
        FoundationCurrentUser currentUser,
        IdentityDtoMapper mapper)
    {
        _users = users;
        _permissionProvider = permissionProvider;
        _currentUser = currentUser;
        _mapper = mapper;
    }

    public async Task<Result<UserProfileDto>> Handle(
        GetCurrentUserQuery request,
        CancellationToken cancellationToken)
    {
        if (_currentUser.UserId is null)
        {
            return Result.Failure<UserProfileDto>("کاربر جاری قابل تشخیص نیست.", "IDENTITY_CURRENT_USER_MISSING");
        }

        var user = await _users.GetByIdWithAccessAsync(_currentUser.UserId.Value, cancellationToken);
        if (user is null)
        {
            return Result.Failure<UserProfileDto>("کاربر یافت نشد.", "IDENTITY_USER_NOT_FOUND");
        }

        var permissions = await _permissionProvider.GetPermissionsAsync(user.Id, cancellationToken);
        return Result.Success(_mapper.ToUserProfile(user, permissions));
    }
}
