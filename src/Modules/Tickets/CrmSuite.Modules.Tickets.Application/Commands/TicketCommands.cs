using CrmSuite.BuildingBlocks.Domain.Results;
using CrmSuite.Modules.Tickets.Application.Dtos;
using CrmSuite.Modules.Tickets.Domain.Enums;
using MediatR;

namespace CrmSuite.Modules.Tickets.Application.Commands;

public sealed record CreateTicketCommand(
    Guid? CustomerId,
    Guid? ServiceId,
    string Subject,
    Guid DepartmentId,
    TicketPriority Priority,
    TicketSource Source,
    string Message,
    Guid? AssignedToUserId = null) : IRequest<Result<TicketDto>>;

public sealed record AddTicketMessageCommand(
    Guid TicketId,
    string Message,
    bool IsInternalNote,
    TicketSenderType SenderType) : IRequest<Result<TicketDto>>;

public sealed record ChangeTicketStatusCommand(
    Guid TicketId,
    TicketStatus Status,
    string? Note = null) : IRequest<Result<TicketDto>>;

public sealed record AssignTicketCommand(
    Guid TicketId,
    Guid? AssignedToUserId) : IRequest<Result<TicketDto>>;
