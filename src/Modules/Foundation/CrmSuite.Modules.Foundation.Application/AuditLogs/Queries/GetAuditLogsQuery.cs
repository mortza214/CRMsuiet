using CrmSuite.BuildingBlocks.Domain.Results;
using CrmSuite.Modules.Foundation.Application.Dtos;
using CrmSuite.Modules.Foundation.Domain.Enums;
using MediatR;

namespace CrmSuite.Modules.Foundation.Application.AuditLogs.Queries;

public sealed record GetAuditLogsQuery(
    int Page = 1,
    int PageSize = 20,
    string? EntityName = null,
    AuditAction? Action = null,
    DateTime? FromUtc = null,
    DateTime? ToUtc = null) : IRequest<Result<PagedResult<AuditLogDto>>>;
