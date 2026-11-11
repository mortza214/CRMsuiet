using CrmSuite.BuildingBlocks.Application.Abstractions;
using CrmSuite.BuildingBlocks.Domain.Results;
using CrmSuite.Modules.Foundation.Application.Abstractions;
using CrmSuite.Modules.Identity.Application.Abstractions;
using CrmSuite.Modules.Identity.Application.Mapping;
using MediatR;
using FoundationCurrentUser = CrmSuite.Modules.Foundation.Application.Abstractions.ICurrentUserService;

namespace CrmSuite.Modules.Identity.Application.Users.Commands;

public sealed class DeleteUserCommandHandler : IRequestHandler<DeleteUserCommand, Result>
{
    private readonly IUserRepository _users;
    private readonly IRefreshTokenRepository _refreshTokens;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IAuditScope _auditScope;
    private readonly FoundationCurrentUser _currentUser;
    private readonly IDateTimeProvider _clock;
    private readonly IdentityDtoMapper _mapper;

    public DeleteUserCommandHandler(
        IUserRepository users,
        IRefreshTokenRepository refreshTokens,
        IUnitOfWork unitOfWork,
        IAuditScope auditScope,
        FoundationCurrentUser currentUser,
        IDateTimeProvider clock,
        IdentityDtoMapper mapper)
    {
        _users = users;
        _refreshTokens = refreshTokens;
        _unitOfWork = unitOfWork;
        _auditScope = auditScope;
        _currentUser = currentUser;
        _clock = clock;
        _mapper = mapper;
    }

    public async Task<Result> Handle(
        DeleteUserCommand request,
        CancellationToken cancellationToken)
    {
        if (_currentUser.UserId == request.Id)
        {
            return Result.Failure(
                "کاربر نمی‌تواند حساب خودش را حذف کند.",
                "IDENTITY_SELF_DELETE_FORBIDDEN");
        }

        var user = await _users.GetByIdWithAccessAsync(request.Id, cancellationToken);
        if (user is null)
        {
            return Result.Failure("کاربر یافت نشد.", "IDENTITY_USER_NOT_FOUND");
        }

        _auditScope.OldValues = _mapper.ToUserDto(user);
        _auditScope.EntityId = user.Id.ToString();

        var now = _clock.UtcNow;
        var tokens = await _refreshTokens.GetActiveByUserAsync(user.Id, now, cancellationToken);
        foreach (var token in tokens)
        {
            token.Revoke(now, _currentUser.IpAddress);
        }

        user.MarkDeleted(_currentUser.UserId, now);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success("کاربر حذف شد.");
    }
}
