using CrmSuite.BuildingBlocks.Domain.Results;
using CrmSuite.Modules.Foundation.Application.Dtos;
using CrmSuite.Modules.Foundation.Domain.Enums;

namespace CrmSuite.Modules.Foundation.Application.Abstractions;

public interface IAuditLogReader
{
    Task<PagedResult<AuditLogDto>> GetPagedAsync(
        int page,
        int pageSize,
        string? entityName,
        AuditAction? action,
        DateTime? fromUtc,
        DateTime? toUtc,
        CancellationToken cancellationToken);
}
