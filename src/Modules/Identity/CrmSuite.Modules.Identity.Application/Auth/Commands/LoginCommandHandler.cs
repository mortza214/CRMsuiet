using CrmSuite.BuildingBlocks.Application.Abstractions;
using CrmSuite.BuildingBlocks.Domain.Results;
using CrmSuite.Modules.Foundation.Application.Abstractions;
using CrmSuite.Modules.Foundation.Application.Auditing;
using CrmSuite.Modules.Foundation.Domain.Enums;
using CrmSuite.Modules.Identity.Application.Abstractions;
using CrmSuite.Modules.Identity.Application.Dtos;
using CrmSuite.Modules.Identity.Application.Mapping;
using CrmSuite.Modules.Identity.Domain.Entities;
using MediatR;
using FoundationCurrentUser = CrmSuite.Modules.Foundation.Application.Abstractions.ICurrentUserService;

namespace CrmSuite.Modules.Identity.Application.Auth.Commands;

public sealed class LoginCommandHandler : IRequestHandler<LoginCommand, Result<AuthResultDto>>
{
    private const int MaxFailedAttempts = 5;
    private static readonly TimeSpan LockoutDuration = TimeSpan.FromMinutes(15);

    private readonly IUserRepository _users;
    private readonly IRefreshTokenRepository _refreshTokens;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly IPermissionProvider _permissionProvider;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IAuditService _auditService;
    private readonly FoundationCurrentUser _currentUser;
    private readonly IDateTimeProvider _clock;
    private readonly IdentityDtoMapper _mapper;

    public LoginCommandHandler(
        IUserRepository users,
        IRefreshTokenRepository refreshTokens,
        IPasswordHasher passwordHasher,
        IJwtTokenService jwtTokenService,
        IPermissionProvider permissionProvider,
        IUnitOfWork unitOfWork,
        IAuditService auditService,
        FoundationCurrentUser currentUser,
        IDateTimeProvider clock,
        IdentityDtoMapper mapper)
    {
        _users = users;
        _refreshTokens = refreshTokens;
        _passwordHasher = passwordHasher;
        _jwtTokenService = jwtTokenService;
        _permissionProvider = permissionProvider;
        _unitOfWork = unitOfWork;
        _auditService = auditService;
        _currentUser = currentUser;
        _clock = clock;
        _mapper = mapper;
    }

    public async Task<Result<AuthResultDto>> Handle(
        LoginCommand request,
        CancellationToken cancellationToken)
    {
        var now = _clock.UtcNow;
        var username = request.Username.Trim();
        var user = await _users.GetByUsernameWithAccessAsync(username, cancellationToken);

        if (user is null)
        {
            await RecordFailedLoginAsync(
                null,
                username,
                "کاربر یافت نشد.",
                cancellationToken);

            return InvalidCredentials();
        }

        if (!user.IsActive)
        {
            await RecordFailedLoginAsync(
                user,
                user.Username,
                "حساب کاربری غیرفعال است.",
                cancellationToken);

            return Result.Failure<AuthResultDto>(
                "حساب کاربری غیرفعال است. با مدیر سیستم تماس بگیرید.",
                "IDENTITY_USER_INACTIVE");
        }

        if (user.IsLockedOut(now))
        {
            await RecordFailedLoginAsync(
                user,
                user.Username,
                "حساب کاربری قفل است.",
                cancellationToken);

            return Result.Failure<AuthResultDto>(
                BuildLockoutMessage(user, now),
                "IDENTITY_USER_LOCKED");
        }

        if (!_passwordHasher.Verify(request.Password, user.PasswordHash))
        {
            user.RegisterFailedLogin(now, MaxFailedAttempts, LockoutDuration);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            await RecordFailedLoginAsync(
                user,
                user.Username,
                "رمز عبور اشتباه است.",
                cancellationToken);

            return user.IsLockedOut(now)
                ? Result.Failure<AuthResultDto>(
                    "حساب کاربری به دلیل ۵ تلاش ناموفق برای ۱۵ دقیقه قفل شد.",
                    "IDENTITY_USER_LOCKED")
                : InvalidCredentials();
        }

        user.RegisterSuccessfulLogin(user.Id, now);

        var roles = GetRoleNames(user);
        var permissions = await _permissionProvider.GetPermissionsAsync(user.Id, cancellationToken);
        var tokens = _jwtTokenService.CreateTokens(user, roles, permissions);

        _refreshTokens.Add(new RefreshToken(
            tokens.RefreshToken.Token,
            user.Id,
            tokens.RefreshToken.ExpiresAtUtc,
            _currentUser.IpAddress,
            now));

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        await _auditService.RecordAsync(
            new AuditEntry(
                "Auth",
                user.Id.ToString(),
                AuditAction.LoginSucceeded,
                null,
                new { user.Username, roles },
                user.Id),
            cancellationToken);

        return Result.Success(_mapper.ToAuthResult(tokens, user, permissions));
    }

    private async Task RecordFailedLoginAsync(
        User? user,
        string username,
        string reason,
        CancellationToken cancellationToken)
    {
        await _auditService.RecordAsync(
            new AuditEntry(
                "Auth",
                username,
                AuditAction.LoginFailed,
                null,
                new { username, reason },
                user?.Id),
            cancellationToken);
    }

    private static Result<AuthResultDto> InvalidCredentials() =>
        Result.Failure<AuthResultDto>(
            "نام کاربری یا رمز عبور اشتباه است.",
            "IDENTITY_INVALID_CREDENTIALS");

    private static string BuildLockoutMessage(User user, DateTime now)
    {
        var remainingMinutes = user.LockoutEnd.HasValue
            ? Math.Max(1, (int)Math.Ceiling((user.LockoutEnd.Value - now).TotalMinutes))
            : 15;

        return $"حساب کاربری تا {remainingMinutes} دقیقه دیگر قفل است.";
    }

    private static IReadOnlyCollection<string> GetRoleNames(User user) =>
        user.UserRoles
            .Where(item => item.Role is not null)
            .Select(item => item.Role.Name)
            .Distinct(StringComparer.Ordinal)
            .ToArray();
}
