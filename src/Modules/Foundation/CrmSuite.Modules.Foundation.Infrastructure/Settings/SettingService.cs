using CrmSuite.BuildingBlocks.Application.Abstractions;
using CrmSuite.BuildingBlocks.Domain.Results;
using CrmSuite.BuildingBlocks.Infrastructure.Persistence;
using CrmSuite.Modules.Foundation.Application.Abstractions;
using CrmSuite.Modules.Foundation.Application.Dtos;
using CrmSuite.Modules.Foundation.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using FoundationCurrentUser = CrmSuite.Modules.Foundation.Application.Abstractions.ICurrentUserService;

namespace CrmSuite.Modules.Foundation.Infrastructure.Settings;

public sealed class SettingService : ISettingService
{
    private readonly AppDbContext _dbContext;
    private readonly FoundationCurrentUser _currentUser;
    private readonly IDateTimeProvider _clock;
    private readonly IJalaliDateService _jalaliDate;

    public SettingService(
        AppDbContext dbContext,
        FoundationCurrentUser currentUser,
        IDateTimeProvider clock,
        IJalaliDateService jalaliDate)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
        _clock = clock;
        _jalaliDate = jalaliDate;
    }

    public async Task<SettingDto?> GetAsync(
        string key,
        bool includeSecret = false,
        CancellationToken cancellationToken = default)
    {
        var setting = await _dbContext.Set<Setting>()
            .AsNoTracking()
            .SingleOrDefaultAsync(item => item.Key == key, cancellationToken);

        return setting is null ? null : Map(setting, includeSecret);
    }

    public async Task<IReadOnlyList<SettingDto>> GetGroupAsync(
        string? group,
        CancellationToken cancellationToken = default)
    {
        var query = _dbContext.Set<Setting>().AsNoTracking();

        if (!string.IsNullOrWhiteSpace(group))
        {
            query = query.Where(item => item.Group == group);
        }

        var settings = await query
            .OrderBy(item => item.Group)
            .ThenBy(item => item.Key)
            .ToListAsync(cancellationToken);

        return settings.Select(item => Map(item, includeSecret: false)).ToArray();
    }

    public async Task<Result<SettingDto>> SetAsync(
        string key,
        string value,
        string group,
        bool isSecret,
        CancellationToken cancellationToken = default)
    {
        var now = _clock.UtcNow;
        var setting = await _dbContext.Set<Setting>()
            .SingleOrDefaultAsync(item => item.Key == key, cancellationToken);

        if (setting is null)
        {
            setting = new Setting(key, value, group, isSecret);
            setting.MarkCreated(_currentUser.UserId, now);
            _dbContext.Set<Setting>().Add(setting);
        }
        else
        {
            setting.Update(value, group, isSecret, _currentUser.UserId, now);
        }

        try
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            return Result.Failure<SettingDto>(
                "ذخیره تنظیمات انجام نشد؛ ممکن است کلید تکراری باشد.",
                "FOUNDATION_SETTING_SAVE_FAILED");
        }

        return Result.Success(Map(setting, includeSecret: false));
    }

    private SettingDto Map(Setting setting, bool includeSecret)
    {
        var createdAt = DateTime.SpecifyKind(setting.CreatedAt, DateTimeKind.Utc);
        var updatedAt = setting.UpdatedAt.HasValue
            ? DateTime.SpecifyKind(setting.UpdatedAt.Value, DateTimeKind.Utc)
            : (DateTime?)null;

        var value = setting.IsSecret && !includeSecret ? null : setting.Value;

        return new SettingDto(
            setting.Key,
            value,
            setting.Group,
            setting.IsSecret,
            createdAt,
            _jalaliDate.ToJalali(createdAt),
            updatedAt,
            updatedAt.HasValue ? _jalaliDate.ToJalali(updatedAt.Value) : null);
    }
}
