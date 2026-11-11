using CrmSuite.BuildingBlocks.Application.Abstractions;
using CrmSuite.BuildingBlocks.Domain.Results;
using CrmSuite.Modules.Foundation.Application.Abstractions;
using CrmSuite.Modules.Settings.Application.Abstractions;
using CrmSuite.Modules.Settings.Application.Dtos;
using CrmSuite.Modules.Settings.Application.Mapping;
using CrmSuite.Modules.Settings.Domain.Entities;
using CrmSuite.Modules.Settings.Domain.Enums;
using MediatR;
using FoundationCurrentUser = CrmSuite.Modules.Foundation.Application.Abstractions.ICurrentUserService;

namespace CrmSuite.Modules.Settings.Application.Commands;

public sealed class UpdateSettingsCommandHandler
    : IRequestHandler<UpdateSettingsCommand, Result<IReadOnlyList<SettingValueDto>>>
{
    private readonly ISettingService _settings;

    public UpdateSettingsCommandHandler(ISettingService settings)
    {
        _settings = settings;
    }

    public async Task<Result<IReadOnlyList<SettingValueDto>>> Handle(
        UpdateSettingsCommand request,
        CancellationToken cancellationToken)
    {
        var results = new List<SettingValueDto>();

        foreach (var item in request.Items)
        {
            var result = await _settings.SetAsync(
                item.Key,
                item.Value,
                item.Group,
                item.IsSecret,
                cancellationToken);

            if (result.IsFailure || result.Value is null)
            {
                return Result.Failure<IReadOnlyList<SettingValueDto>>(
                    result.Errors,
                    result.Message ?? "ذخیره تنظیمات ناموفق بود.");
            }

            results.Add(new SettingValueDto(
                result.Value.Key,
                result.Value.Value ?? string.Empty,
                result.Value.Group,
                result.Value.IsSecret));
        }

        return Result.Success<IReadOnlyList<SettingValueDto>>(results);
    }
}

public sealed class CreateTaxRuleCommandHandler
    : IRequestHandler<CreateTaxRuleCommand, Result<TaxRuleDto>>
{
    private readonly ITaxRuleRepository _rules;
    private readonly ISettingsUnitOfWork _unitOfWork;
    private readonly IDateTimeProvider _clock;
    private readonly FoundationCurrentUser _currentUser;
    private readonly SettingsDtoMapper _mapper;

    public CreateTaxRuleCommandHandler(
        ITaxRuleRepository rules,
        ISettingsUnitOfWork unitOfWork,
        IDateTimeProvider clock,
        FoundationCurrentUser currentUser,
        SettingsDtoMapper mapper)
    {
        _rules = rules;
        _unitOfWork = unitOfWork;
        _clock = clock;
        _currentUser = currentUser;
        _mapper = mapper;
    }

    public async Task<Result<TaxRuleDto>> Handle(
        CreateTaxRuleCommand request,
        CancellationToken cancellationToken)
    {
        var rule = new TaxRule(
            request.Name,
            request.Percentage,
            request.IsActive,
            request.EffectiveFromUtc,
            _currentUser.UserId,
            _clock.UtcNow);

        _rules.Add(rule);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(_mapper.ToTaxRuleDto(rule));
    }
}

public sealed class UpdateTaxRuleCommandHandler
    : IRequestHandler<UpdateTaxRuleCommand, Result<TaxRuleDto>>
{
    private readonly ITaxRuleRepository _rules;
    private readonly ISettingsUnitOfWork _unitOfWork;
    private readonly IDateTimeProvider _clock;
    private readonly FoundationCurrentUser _currentUser;
    private readonly SettingsDtoMapper _mapper;

    public UpdateTaxRuleCommandHandler(
        ITaxRuleRepository rules,
        ISettingsUnitOfWork unitOfWork,
        IDateTimeProvider clock,
        FoundationCurrentUser currentUser,
        SettingsDtoMapper mapper)
    {
        _rules = rules;
        _unitOfWork = unitOfWork;
        _clock = clock;
        _currentUser = currentUser;
        _mapper = mapper;
    }

    public async Task<Result<TaxRuleDto>> Handle(
        UpdateTaxRuleCommand request,
        CancellationToken cancellationToken)
    {
        var rule = await _rules.GetByIdAsync(request.Id, cancellationToken);
        if (rule is null)
        {
            return Result.Failure<TaxRuleDto>("قانون مالیاتی یافت نشد.", "SETTINGS_TAX_NOT_FOUND");
        }

        rule.Update(
            request.Name,
            request.Percentage,
            request.IsActive,
            request.EffectiveFromUtc,
            _currentUser.UserId,
            _clock.UtcNow);

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success(_mapper.ToTaxRuleDto(rule));
    }
}

public sealed class UpdateGatewayConfigCommandHandler
    : IRequestHandler<UpdateGatewayConfigCommand, Result<GatewayConfigDto>>
{
    private readonly IGatewayConfigRepository _configs;
    private readonly IEncryptionService _encryption;
    private readonly ISettingsUnitOfWork _unitOfWork;
    private readonly IDateTimeProvider _clock;
    private readonly FoundationCurrentUser _currentUser;
    private readonly SettingsDtoMapper _mapper;

    public UpdateGatewayConfigCommandHandler(
        IGatewayConfigRepository configs,
        IEncryptionService encryption,
        ISettingsUnitOfWork unitOfWork,
        IDateTimeProvider clock,
        FoundationCurrentUser currentUser,
        SettingsDtoMapper mapper)
    {
        _configs = configs;
        _encryption = encryption;
        _unitOfWork = unitOfWork;
        _clock = clock;
        _currentUser = currentUser;
        _mapper = mapper;
    }

    public async Task<Result<GatewayConfigDto>> Handle(
        UpdateGatewayConfigCommand request,
        CancellationToken cancellationToken)
    {
        var merchantEncrypted = _encryption.Encrypt(request.MerchantId);
        var terminalEncrypted = _encryption.Encrypt(request.TerminalId);
        var config = await _configs.GetByProviderAsync(request.Provider, cancellationToken);

        if (config is null)
        {
            config = new GatewayConfig(
                request.Provider,
                request.IsActive,
                merchantEncrypted,
                terminalEncrypted,
                _currentUser.UserId,
                _clock.UtcNow);

            _configs.Add(config);
        }
        else
        {
            config.Update(
                request.IsActive,
                merchantEncrypted,
                terminalEncrypted,
                _currentUser.UserId,
                _clock.UtcNow);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success(_mapper.ToGatewayDto(config, _encryption));
    }
}
