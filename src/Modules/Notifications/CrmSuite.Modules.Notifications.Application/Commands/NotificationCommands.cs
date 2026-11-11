using CrmSuite.BuildingBlocks.Domain.Results;
using CrmSuite.Modules.Notifications.Application.Dtos;
using CrmSuite.Modules.Notifications.Domain.Enums;
using MediatR;

namespace CrmSuite.Modules.Notifications.Application.Commands;

public sealed record SendNotificationCommand(
    string TemplateCode,
    string Recipient,
    NotificationChannel Channel,
    IReadOnlyDictionary<string, string> Variables) : IRequest<Result<SendNotificationResultDto>>;

public sealed record MarkNotificationReadCommand(Guid Id) : IRequest<Result<bool>>;
