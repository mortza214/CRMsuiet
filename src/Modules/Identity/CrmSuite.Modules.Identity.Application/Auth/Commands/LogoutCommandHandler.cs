using CrmSuite.BuildingBlocks.Application.Abstractions;
using CrmSuite.BuildingBlocks.Domain.Results;
using CrmSuite.Modules.Foundation.Application.Abstractions;
using CrmSuite.Modules.Foundation.Application.Auditing;
using CrmSuite.Modules.Foundation.Domain.Enums;
using CrmSuite.Modules.Identity.Application.Abstractions;
using MediatR;
using FoundationCurrentUser = CrmSuite.Modules.Foundation.Application.Abstractions.ICurrentUserService;

namespace CrmSuite.Modules.Identity.Application.Auth.Commands;

public sealed class LogoutCommandHandler : IRequestHandler<LogoutCommand, Result>
{
    private readonly IRefreshTokenRepository _refreshTokens;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IAuditService _auditService;
    private readonly FoundationCurrentUser _currentUser;
    private readonly IDateTimeProvider _clock;

    public LogoutCommandHandler(
        IRefreshTokenRepository refreshTokens,
        IUnitOfWork unitOfWork,
        IAuditService auditService,
        FoundationCurrentUser currentUser,
        IDateTimeProvider clock)
    {
        _refreshTokens = refreshTokens;
        _unitOfWork = unitOfWork;
        _auditService = auditService;
        _currentUser = currentUser;
        _clock = clock;
    }

    public async Task<Result> Handle(
        LogoutCommand request,
        CancellationToken cancellationToken)
    {
        var storedToken = await _refreshTokens.GetByTokenAsync(request.RefreshToken, cancellationToken);

        if (storedToken is null)
        {
            return Result.Success("خروج انجام شد.");
        }

        if (storedToken.RevokedAt is null)
        {
            storedToken.Revoke(_clock.UtcNow, _currentUser.IpAddress);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            await _auditService.RecordAsync(
                new AuditEntry(
                    "Auth",
                    storedToken.UserId.ToString(),
                    AuditAction.Logout,
                    null,
                    new { Message = "کاربر از حساب خارج شد." },
                    storedToken.UserId),
                cancellationToken);
        }

        return Result.Success("خروج انجام شد.");
    }
}
