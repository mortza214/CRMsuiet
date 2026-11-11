using CrmSuite.BuildingBlocks.Domain.Results;
using CrmSuite.Modules.Foundation.Application.Abstractions;
using CrmSuite.Modules.Foundation.Application.Dtos;
using MediatR;

namespace CrmSuite.Modules.Foundation.Application.AuditLogs.Queries;

public sealed class GetAuditLogsQueryHandler
    : IRequestHandler<GetAuditLogsQuery, Result<PagedResult<AuditLogDto>>>
{
    private readonly IAuditLogReader _reader;

    public GetAuditLogsQueryHandler(IAuditLogReader reader)
    {
        _reader = reader;
    }

    public async Task<Result<PagedResult<AuditLogDto>>> Handle(
        GetAuditLogsQuery request,
        CancellationToken cancellationToken)
    {
        var page = await _reader.GetPagedAsync(
            request.Page,
            request.PageSize,
            request.EntityName,
            request.Action,
            request.FromUtc,
            request.ToUtc,
            cancellationToken);

        return Result.Success(page);
    }
}
