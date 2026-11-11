using System.Text.RegularExpressions;
using CrmSuite.Modules.Notifications.Application.Abstractions;
using CrmSuite.Modules.Notifications.Domain.Entities;
using CrmSuite.Modules.Notifications.Domain.Enums;

namespace CrmSuite.Modules.Notifications.Infrastructure.Services;

public sealed partial class NotificationService : INotificationService
{
    private readonly INotificationTemplateRepository _templates;
    private readonly INotificationLogRepository _logs;
    private readonly INotificationUnitOfWork _unitOfWork;
    private readonly IReadOnlyDictionary<NotificationChannel, INotificationSender> _senders;

    public NotificationService(
        INotificationTemplateRepository templates,
        INotificationLogRepository logs,
        INotificationUnitOfWork unitOfWork,
        IEnumerable<INotificationSender> senders)
    {
        _templates = templates;
        _logs = logs;
        _unitOfWork = unitOfWork;
        _senders = senders.ToDictionary(sender => sender.Channel);
    }

    public async Task<NotificationSendResult> SendAsync(
        NotificationRequest request,
        CancellationToken cancellationToken)
    {
        var template = await _templates.GetByCodeAsync(request.TemplateCode, cancellationToken);
        if (template is null)
        {
            return new NotificationSendResult(
                Guid.Empty,
                string.Empty,
                NotificationStatus.Failed,
                $"قالب اعلان {request.TemplateCode} یافت نشد.");
        }

        var content = Render(template.Body, request.Variables);
        var log = new NotificationLog(
            request.Recipient,
            request.Channel,
            request.TemplateCode,
            content,
            request.CreatedByUserId,
            DateTime.UtcNow);

        _logs.Add(log);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        if (!_senders.TryGetValue(request.Channel, out var sender))
        {
            log.MarkFailed("Sender برای این کانال ثبت نشده است.", DateTime.UtcNow);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            return new NotificationSendResult(log.Id, content, NotificationStatus.Failed, log.ErrorMessage);
        }

        try
        {
            await sender.SendAsync(request.Recipient, content, cancellationToken);
            log.MarkSent(DateTime.UtcNow);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return new NotificationSendResult(log.Id, content, NotificationStatus.Sent, null);
        }
        catch (Exception exception)
        {
            log.MarkFailed(exception.Message, DateTime.UtcNow);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return new NotificationSendResult(log.Id, content, NotificationStatus.Failed, exception.Message);
        }
    }

    private static string Render(
        string body,
        IReadOnlyDictionary<string, string> variables) =>
        PlaceholderRegex().Replace(body, match =>
        {
            var key = match.Groups[1].Value;
            return variables.TryGetValue(key, out var value) ? value : match.Value;
        });

    [GeneratedRegex(@"\{([^{}]+)\}")]
    private static partial Regex PlaceholderRegex();
}
