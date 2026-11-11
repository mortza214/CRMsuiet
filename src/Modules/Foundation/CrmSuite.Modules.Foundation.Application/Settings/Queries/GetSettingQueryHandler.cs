using CrmSuite.BuildingBlocks.Domain.Results;
using CrmSuite.Modules.Foundation.Application.Abstractions;
using CrmSuite.Modules.Foundation.Application.Dtos;
using MediatR;

namespace CrmSuite.Modules.Foundation.Application.Settings.Queries;

public sealed class GetSettingQueryHandler : IRequestHandler<GetSettingQuery, Result<SettingDto>>
{
    private readonly ISettingService _settingService;

    public GetSettingQueryHandler(ISettingService settingService)
    {
        _settingService = settingService;
    }

    public async Task<Result<SettingDto>> Handle(
        GetSettingQuery request,
        CancellationToken cancellationToken)
    {
        var setting = await _settingService.GetAsync(request.Key, includeSecret: false, cancellationToken);

        return setting is null
            ? Result.Failure<SettingDto>("تنظیمات درخواستی یافت نشد.", "تنظیمات یافت نشد.")
            : Result.Success(setting);
    }
}
