using CrmSuite.BuildingBlocks.Domain.Results;
using CrmSuite.Modules.Notifications.Application.Dtos;
using MediatR;

namespace CrmSuite.Modules.Notifications.Application.Queries;

public sealed record GetMyNotificationsQuery : IRequest<Result<IReadOnlyList<NotificationDto>>>;
