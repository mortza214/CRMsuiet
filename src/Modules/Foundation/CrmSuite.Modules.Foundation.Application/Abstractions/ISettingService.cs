using CrmSuite.BuildingBlocks.Domain.Results;
using CrmSuite.Modules.Foundation.Application.Dtos;

namespace CrmSuite.Modules.Foundation.Application.Abstractions;

public interface ISettingService
{
    Task<SettingDto?> GetAsync(
        string key,
        bool includeSecret = false,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<SettingDto>> GetGroupAsync(
        string? group,
        CancellationToken cancellationToken = default);

    Task<Result<SettingDto>> SetAsync(
        string key,
        string value,
        string group,
        bool isSecret,
        CancellationToken cancellationToken = default);
}
