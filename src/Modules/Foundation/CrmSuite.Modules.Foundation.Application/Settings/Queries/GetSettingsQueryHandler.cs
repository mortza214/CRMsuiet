using CrmSuite.BuildingBlocks.Domain.Results;
using CrmSuite.Modules.Foundation.Application.Abstractions;
using CrmSuite.Modules.Foundation.Application.Dtos;
using MediatR;

namespace CrmSuite.Modules.Foundation.Application.Settings.Queries;

public sealed class GetSettingsQueryHandler
    : IRequestHandler<GetSettingsQuery, Result<IReadOnlyList<SettingDto>>>
{
    private readonly ISettingService _settingService;

    public GetSettingsQueryHandler(ISettingService settingService)
    {
        _settingService = settingService;
    }

    public async Task<Result<IReadOnlyList<SettingDto>>> Handle(
        GetSettingsQuery request,
        CancellationToken cancellationToken)
    {
        var settings = await _settingService.GetGroupAsync(request.Group, cancellationToken);
        return Result.Success(settings);
    }
}
