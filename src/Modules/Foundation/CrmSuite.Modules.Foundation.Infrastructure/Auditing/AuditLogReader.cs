using CrmSuite.BuildingBlocks.Domain.Results;
using CrmSuite.BuildingBlocks.Infrastructure.Persistence;
using CrmSuite.Modules.Foundation.Application.Abstractions;
using CrmSuite.Modules.Foundation.Application.Dtos;
using CrmSuite.Modules.Foundation.Domain.Entities;
using CrmSuite.Modules.Foundation.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace CrmSuite.Modules.Foundation.Infrastructure.Auditing;

public sealed class AuditLogReader : IAuditLogReader
{
    private readonly AppDbContext _dbContext;
    private readonly IJalaliDateService _jalaliDate;

    public AuditLogReader(AppDbContext dbContext, IJalaliDateService jalaliDate)
    {
        _dbContext = dbContext;
        _jalaliDate = jalaliDate;
    }

    public async Task<PagedResult<AuditLogDto>> GetPagedAsync(
        int page,
        int pageSize,
        string? entityName,
        AuditAction? action,
        DateTime? fromUtc,
        DateTime? toUtc,
        CancellationToken cancellationToken)
    {
        var query = _dbContext.Set<AuditLog>().AsNoTracking();

        if (!string.IsNullOrWhiteSpace(entityName))
        {
            query = query.Where(log => log.EntityName == entityName);
        }

        if (action.HasValue)
        {
            query = query.Where(log => log.Action == action.Value);
        }

        if (fromUtc.HasValue)
        {
            query = query.Where(log => log.Timestamp >= fromUtc.Value);
        }

        if (toUtc.HasValue)
        {
            query = query.Where(log => log.Timestamp <= toUtc.Value);
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var logs = await query
            .OrderByDescending(log => log.Timestamp)
            .ThenByDescending(log => log.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        var items = logs.Select(log =>
        {
            var timestamp = DateTime.SpecifyKind(log.Timestamp, DateTimeKind.Utc);

            return new AuditLogDto(
                log.Id,
                log.EntityName,
                log.EntityId,
                log.Action.ToString(),
                log.OldValues,
                log.NewValues,
                log.UserId,
                log.UserIp,
                timestamp,
                _jalaliDate.ToJalali(timestamp));
        }).ToArray();

        return new PagedResult<AuditLogDto>(items, totalCount, page, pageSize);
    }
}
