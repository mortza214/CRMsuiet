using CrmSuite.BuildingBlocks.Domain.Results;
using CrmSuite.Modules.Foundation.Application.Dtos;
using MediatR;

namespace CrmSuite.Modules.Foundation.Application.Settings.Queries;

public sealed record GetSettingsQuery(string? Group = null) : IRequest<Result<IReadOnlyList<SettingDto>>>;
