using CrmSuite.BuildingBlocks.Application.Abstractions;
using CrmSuite.BuildingBlocks.Domain.Results;
using CrmSuite.Modules.Foundation.Application.Abstractions;
using CrmSuite.Modules.Identity.Application.Abstractions;
using CrmSuite.Modules.Identity.Application.Dtos;
using CrmSuite.Modules.Identity.Application.Mapping;
using CrmSuite.Modules.Identity.Domain.Entities;
using MediatR;
using FoundationCurrentUser = CrmSuite.Modules.Foundation.Application.Abstractions.ICurrentUserService;

namespace CrmSuite.Modules.Identity.Application.Users.Commands;

public sealed class UpdateUserCommandHandler : IRequestHandler<UpdateUserCommand, Result<UserDto>>
{
    private readonly IUserRepository _users;
    private readonly IRoleRepository _roles;
    private readonly IDepartmentRepository _departments;
    private readonly IRefreshTokenRepository _refreshTokens;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IPermissionProvider _permissionProvider;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IAuditScope _auditScope;
    private readonly FoundationCurrentUser _currentUser;
    private readonly IDateTimeProvider _clock;
    private readonly IdentityDtoMapper _mapper;

    public UpdateUserCommandHandler(
        IUserRepository users,
        IRoleRepository roles,
        IDepartmentRepository departments,
        IRefreshTokenRepository refreshTokens,
        IPasswordHasher passwordHasher,
        IPermissionProvider permissionProvider,
        IUnitOfWork unitOfWork,
        IAuditScope auditScope,
        FoundationCurrentUser currentUser,
        IDateTimeProvider clock,
        IdentityDtoMapper mapper)
    {
        _users = users;
        _roles = roles;
        _departments = departments;
        _refreshTokens = refreshTokens;
        _passwordHasher = passwordHasher;
        _permissionProvider = permissionProvider;
        _unitOfWork = unitOfWork;
        _auditScope = auditScope;
        _currentUser = currentUser;
        _clock = clock;
        _mapper = mapper;
    }

    public async Task<Result<UserDto>> Handle(
        UpdateUserCommand request,
        CancellationToken cancellationToken)
    {
        var user = await _users.GetByIdWithAccessAsync(request.Id, cancellationToken);
        if (user is null)
        {
            return Result.Failure<UserDto>("کاربر یافت نشد.", "IDENTITY_USER_NOT_FOUND");
        }

        if (await _users.MobileExistsAsync(request.Mobile, request.Id, cancellationToken))
        {
            return Result.Failure<UserDto>("موبایل قبلاً استفاده شده است.", "IDENTITY_MOBILE_DUPLICATE");
        }

        var roleIds = request.RoleIds?.Distinct().ToArray();
        var departmentIds = request.DepartmentIds?.Distinct().ToArray();

        if (roleIds is { Length: > 0 })
        {
            var roles = await _roles.GetByIdsAsync(roleIds, false, cancellationToken);
            if (roles.Count != roleIds.Length)
            {
                return Result.Failure<UserDto>("یکی از نقش‌های انتخاب‌شده معتبر نیست.");
            }
        }

        if (departmentIds is { Length: > 0 })
        {
            var departments = await _departments.GetByIdsAsync(departmentIds, cancellationToken);
            if (departments.Count != departmentIds.Length)
            {
                return Result.Failure<UserDto>("یکی از دپارتمان‌های انتخاب‌شده معتبر نیست.");
            }
        }

        _auditScope.OldValues = _mapper.ToUserDto(user);
        _auditScope.EntityId = user.Id.ToString();

        var now = _clock.UtcNow;
        var wasActive = user.IsActive;
        user.UpdateProfile(
            request.FullName,
            request.Mobile,
            request.Email,
            request.IsActive,
            _currentUser.UserId,
            now);

        if (!string.IsNullOrWhiteSpace(request.NewPassword))
        {
            user.SetPasswordHash(_passwordHasher.Hash(request.NewPassword), _currentUser.UserId, now);
        }

        if (roleIds is not null)
        {
            user.ReplaceRoles(roleIds, _currentUser.UserId, now);
            _permissionProvider.Invalidate(user.Id);
        }

        if (departmentIds is not null)
        {
            user.ReplaceDepartments(departmentIds, _currentUser.UserId, now);
        }

        if (wasActive && !user.IsActive)
        {
            await RevokeActiveTokensAsync(user.Id, now, cancellationToken);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var updated = await _users.GetByIdWithAccessAsync(user.Id, cancellationToken);
        return Result.Success(_mapper.ToUserDto(updated ?? user));
    }

    private async Task RevokeActiveTokensAsync(
        Guid userId,
        DateTime utcNow,
        CancellationToken cancellationToken)
    {
        var tokens = await _refreshTokens.GetActiveByUserAsync(userId, utcNow, cancellationToken);
        foreach (var token in tokens)
        {
            token.Revoke(utcNow, _currentUser.IpAddress);
        }
    }
}
