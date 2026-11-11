using CrmSuite.BuildingBlocks.Domain.Results;
using CrmSuite.Modules.Notifications.Application.Abstractions;
using CrmSuite.Modules.Notifications.Application.Dtos;
using CrmSuite.Modules.Notifications.Domain.Enums;
using MediatR;
using FoundationCurrentUser = CrmSuite.Modules.Foundation.Application.Abstractions.ICurrentUserService;

namespace CrmSuite.Modules.Notifications.Application.Commands;

public sealed class SendNotificationCommandHandler
    : IRequestHandler<SendNotificationCommand, Result<SendNotificationResultDto>>
{
    private readonly INotificationService _notifications;

    public SendNotificationCommandHandler(INotificationService notifications)
    {
        _notifications = notifications;
    }

    public async Task<Result<SendNotificationResultDto>> Handle(
        SendNotificationCommand request,
        CancellationToken cancellationToken)
    {
        var result = await _notifications.SendAsync(
            new NotificationRequest(
                request.TemplateCode,
                request.Recipient,
                request.Channel,
                request.Variables),
            cancellationToken);

        var dto = new SendNotificationResultDto(
            result.LogId,
            result.Content,
            result.Status.ToString(),
            result.Error);

        return result.Status == NotificationStatus.Failed
            ? Result.Failure<SendNotificationResultDto>(
                result.Error ?? "ارسال اعلان ناموفق بود.",
                "NOTIFICATIONS_SEND_FAILED")
            : Result.Success(dto);
    }
}

public sealed class MarkNotificationReadCommandHandler
    : IRequestHandler<MarkNotificationReadCommand, Result<bool>>
{
    private readonly INotificationLogRepository _logs;
    private readonly INotificationUnitOfWork _unitOfWork;
    private readonly FoundationCurrentUser _currentUser;

    public MarkNotificationReadCommandHandler(
        INotificationLogRepository logs,
        INotificationUnitOfWork unitOfWork,
        FoundationCurrentUser currentUser)
    {
        _logs = logs;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
    }

    public async Task<Result<bool>> Handle(
        MarkNotificationReadCommand request,
        CancellationToken cancellationToken)
    {
        if (_currentUser.UserId is null)
        {
            return Result.Failure<bool>("کاربر جاری قابل تشخیص نیست.", "NOTIFICATIONS_CURRENT_USER_MISSING");
        }

        var log = await _logs.GetByIdAsync(request.Id, cancellationToken);
        if (log is null || log.Recipient != _currentUser.UserId.Value.ToString())
        {
            return Result.Failure<bool>("اعلان یافت نشد.", "NOTIFICATIONS_NOT_FOUND");
        }

        log.MarkRead();
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(true);
    }
}
