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

public sealed class RefreshTokenCommandHandler
    : IRequestHandler<RefreshTokenCommand, Result<AuthResultDto>>
{
    private readonly IRefreshTokenRepository _refreshTokens;
    private readonly IUserRepository _users;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly IPermissionProvider _permissionProvider;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IAuditService _auditService;
    private readonly FoundationCurrentUser _currentUser;
    private readonly IDateTimeProvider _clock;
    private readonly IdentityDtoMapper _mapper;

    public RefreshTokenCommandHandler(
        IRefreshTokenRepository refreshTokens,
        IUserRepository users,
        IJwtTokenService jwtTokenService,
        IPermissionProvider permissionProvider,
        IUnitOfWork unitOfWork,
        IAuditService auditService,
        FoundationCurrentUser currentUser,
        IDateTimeProvider clock,
        IdentityDtoMapper mapper)
    {
        _refreshTokens = refreshTokens;
        _users = users;
        _jwtTokenService = jwtTokenService;
        _permissionProvider = permissionProvider;
        _unitOfWork = unitOfWork;
        _auditService = auditService;
        _currentUser = currentUser;
        _clock = clock;
        _mapper = mapper;
    }

    public async Task<Result<AuthResultDto>> Handle(
        RefreshTokenCommand request,
        CancellationToken cancellationToken)
    {
        var now = _clock.UtcNow;
        var storedToken = await _refreshTokens.GetByTokenAsync(request.RefreshToken, cancellationToken);

        if (storedToken is null || !storedToken.IsActive(now))
        {
            return Result.Failure<AuthResultDto>(
                "Refresh Token معتبر یا فعال نیست.",
                "IDENTITY_REFRESH_TOKEN_INVALID");
        }

        var user = await _users.GetByIdWithAccessAsync(storedToken.UserId, cancellationToken);

        if (user is null || !user.IsActive)
        {
            storedToken.Revoke(now, _currentUser.IpAddress);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return Result.Failure<AuthResultDto>(
                "کاربر مربوط به این توکن فعال نیست.",
                "IDENTITY_USER_INACTIVE");
        }

        var roles = GetRoleNames(user);
        var permissions = await _permissionProvider.GetPermissionsAsync(user.Id, cancellationToken);
        var tokens = _jwtTokenService.CreateTokens(user, roles, permissions);

        storedToken.Revoke(now, _currentUser.IpAddress, tokens.RefreshToken.Token);
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
                AuditAction.TokenRefreshed,
                null,
                new { user.Username },
                user.Id),
            cancellationToken);

        return Result.Success(_mapper.ToAuthResult(tokens, user, permissions));
    }

    private static IReadOnlyCollection<string> GetRoleNames(User user) =>
        user.UserRoles
            .Where(item => item.Role is not null)
            .Select(item => item.Role.Name)
            .Distinct(StringComparer.Ordinal)
            .ToArray();
}
