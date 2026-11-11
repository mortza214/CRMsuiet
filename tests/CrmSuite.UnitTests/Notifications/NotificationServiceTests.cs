using CrmSuite.Modules.Notifications.Application.Abstractions;
using CrmSuite.Modules.Notifications.Domain.Entities;
using CrmSuite.Modules.Notifications.Domain.Enums;
using CrmSuite.Modules.Notifications.Infrastructure.Senders;
using CrmSuite.Modules.Notifications.Infrastructure.Services;
using FluentAssertions;
using Moq;

namespace CrmSuite.UnitTests.Notifications;

public sealed class NotificationServiceTests
{
    [Fact]
    public async Task Sending_renders_template_and_marks_log_sent()
    {
        var template = new NotificationTemplate(
            "TicketCreated",
            "تیکت",
            "تیکت {TicketNumber} برای {CustomerName} ثبت شد.",
            null,
            DateTime.UtcNow);

        var templates = new Mock<INotificationTemplateRepository>();
        templates.Setup(repository => repository.GetByCodeAsync(
                "TicketCreated",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(template);

        NotificationLog? saved = null;
        var logs = new Mock<INotificationLogRepository>();
        logs.Setup(repository => repository.Add(It.IsAny<NotificationLog>()))
            .Callback<NotificationLog>(log => saved = log);

        var unitOfWork = new Mock<INotificationUnitOfWork>();
        unitOfWork.Setup(work => work.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var service = new NotificationService(
            templates.Object,
            logs.Object,
            unitOfWork.Object,
            [new InAppNotificationSender()]);

        var result = await service.SendAsync(
            new NotificationRequest(
                "TicketCreated",
                Guid.NewGuid().ToString(),
                NotificationChannel.InApp,
                new Dictionary<string, string>
                {
                    ["TicketNumber"] = "TKT-1405-00001",
                    ["CustomerName"] = "مشتری تست",
                }),
            CancellationToken.None);

        result.Status.Should().Be(NotificationStatus.Sent);
        saved.Should().NotBeNull();
        saved!.Status.Should().Be(NotificationStatus.Sent);
        saved.Content.Should().Contain("TKT-1405-00001");
        saved.Content.Should().Contain("مشتری تست");
    }
}
