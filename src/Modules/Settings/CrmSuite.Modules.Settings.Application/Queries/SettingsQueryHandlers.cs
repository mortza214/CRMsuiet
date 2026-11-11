using CrmSuite.BuildingBlocks.Domain.Results;
using CrmSuite.Modules.Foundation.Application.Abstractions;
using CrmSuite.Modules.Settings.Application.Abstractions;
using CrmSuite.Modules.Settings.Application.Dtos;
using CrmSuite.Modules.Settings.Application.Mapping;
using MediatR;

namespace CrmSuite.Modules.Settings.Application.Queries;

public sealed class GetSettingsByGroupQueryHandler
    : IRequestHandler<GetSettingsByGroupQuery, Result<IReadOnlyList<SettingGroupDto>>>
{
    private readonly ISettingService _settings;

    public GetSettingsByGroupQueryHandler(ISettingService settings)
    {
        _settings = settings;
    }

    public async Task<Result<IReadOnlyList<SettingGroupDto>>> Handle(
        GetSettingsByGroupQuery request,
        CancellationToken cancellationToken)
    {
        var settings = await _settings.GetGroupAsync(request.Group, cancellationToken);

        var groups = settings
            .GroupBy(setting => setting.Group)
            .OrderBy(group => group.Key)
            .Select(group => new SettingGroupDto(
                group.Key,
                group.Select(setting => new SettingValueDto(
                    setting.Key,
                    setting.Value ?? string.Empty,
                    setting.Group,
                    setting.IsSecret)).ToArray()))
            .ToArray();

        return Result.Success<IReadOnlyList<SettingGroupDto>>(groups);
    }
}

public sealed class GetTaxRulesQueryHandler
    : IRequestHandler<GetTaxRulesQuery, Result<IReadOnlyList<TaxRuleDto>>>
{
    private readonly ITaxRuleRepository _rules;
    private readonly SettingsDtoMapper _mapper;

    public GetTaxRulesQueryHandler(ITaxRuleRepository rules, SettingsDtoMapper mapper)
    {
        _rules = rules;
        _mapper = mapper;
    }

    public async Task<Result<IReadOnlyList<TaxRuleDto>>> Handle(
        GetTaxRulesQuery request,
        CancellationToken cancellationToken)
    {
        var rules = await _rules.GetAllAsync(cancellationToken);
        return Result.Success<IReadOnlyList<TaxRuleDto>>(
            rules.Select(_mapper.ToTaxRuleDto).ToArray());
    }
}

public sealed class GetGatewayConfigsQueryHandler
    : IRequestHandler<GetGatewayConfigsQuery, Result<IReadOnlyList<GatewayConfigDto>>>
{
    private readonly IGatewayConfigRepository _configs;
    private readonly IEncryptionService _encryption;
    private readonly SettingsDtoMapper _mapper;

    public GetGatewayConfigsQueryHandler(
        IGatewayConfigRepository configs,
        IEncryptionService encryption,
        SettingsDtoMapper mapper)
    {
        _configs = configs;
        _encryption = encryption;
        _mapper = mapper;
    }

    public async Task<Result<IReadOnlyList<GatewayConfigDto>>> Handle(
        GetGatewayConfigsQuery request,
        CancellationToken cancellationToken)
    {
        var configs = await _configs.GetAllAsync(cancellationToken);
        return Result.Success<IReadOnlyList<GatewayConfigDto>>(
            configs.Select(config => _mapper.ToGatewayDto(config, _encryption)).ToArray());
    }
}
