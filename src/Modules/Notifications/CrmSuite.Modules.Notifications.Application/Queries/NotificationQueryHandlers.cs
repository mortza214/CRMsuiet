using CrmSuite.BuildingBlocks.Domain.Results;
using CrmSuite.Modules.Notifications.Application.Abstractions;
using CrmSuite.Modules.Notifications.Application.Dtos;
using CrmSuite.Modules.Notifications.Application.Mapping;
using MediatR;
using FoundationCurrentUser = CrmSuite.Modules.Foundation.Application.Abstractions.ICurrentUserService;

namespace CrmSuite.Modules.Notifications.Application.Queries;

public sealed class GetMyNotificationsQueryHandler
    : IRequestHandler<GetMyNotificationsQuery, Result<IReadOnlyList<NotificationDto>>>
{
    private readonly INotificationLogRepository _logs;
    private readonly FoundationCurrentUser _currentUser;
    private readonly NotificationDtoMapper _mapper;

    public GetMyNotificationsQueryHandler(
        INotificationLogRepository logs,
        FoundationCurrentUser currentUser,
        NotificationDtoMapper mapper)
    {
        _logs = logs;
        _currentUser = currentUser;
        _mapper = mapper;
    }

    public async Task<Result<IReadOnlyList<NotificationDto>>> Handle(
        GetMyNotificationsQuery request,
        CancellationToken cancellationToken)
    {
        if (_currentUser.UserId is null)
        {
            return Result.Failure<IReadOnlyList<NotificationDto>>(
                "کاربر جاری قابل تشخیص نیست.",
                "NOTIFICATIONS_CURRENT_USER_MISSING");
        }

        var logs = await _logs.GetInAppAsync(
            _currentUser.UserId.Value.ToString(),
            cancellationToken);

        return Result.Success<IReadOnlyList<NotificationDto>>(
            logs.Select(_mapper.ToDto).ToArray());
    }
}
