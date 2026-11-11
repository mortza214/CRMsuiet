using CrmSuite.BuildingBlocks.Domain.Results;
using CrmSuite.Modules.Settings.Application.Dtos;
using CrmSuite.Modules.Settings.Domain.Enums;
using MediatR;

namespace CrmSuite.Modules.Settings.Application.Commands;

public sealed record UpdateSettingsCommand(
    IReadOnlyCollection<SettingUpdateItem> Items) : IRequest<Result<IReadOnlyList<SettingValueDto>>>;

public sealed record CreateTaxRuleCommand(
    string Name,
    decimal Percentage,
    bool IsActive,
    DateTime EffectiveFromUtc) : IRequest<Result<TaxRuleDto>>;

public sealed record UpdateTaxRuleCommand(
    Guid Id,
    string Name,
    decimal Percentage,
    bool IsActive,
    DateTime EffectiveFromUtc) : IRequest<Result<TaxRuleDto>>;

public sealed record UpdateGatewayConfigCommand(
    GatewayProvider Provider,
    bool IsActive,
    string MerchantId,
    string TerminalId) : IRequest<Result<GatewayConfigDto>>;
