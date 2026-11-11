using CrmSuite.BuildingBlocks.Domain.Results;
using CrmSuite.Modules.Settings.Application.Dtos;
using MediatR;

namespace CrmSuite.Modules.Settings.Application.Queries;

public sealed record GetSettingsByGroupQuery(string? Group = null)
    : IRequest<Result<IReadOnlyList<SettingGroupDto>>>;

public sealed record GetTaxRulesQuery : IRequest<Result<IReadOnlyList<TaxRuleDto>>>;

public sealed record GetGatewayConfigsQuery : IRequest<Result<IReadOnlyList<GatewayConfigDto>>>;
