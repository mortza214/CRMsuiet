using CrmSuite.BuildingBlocks.Domain.Results;
using CrmSuite.Modules.Foundation.Application.Dtos;
using MediatR;

namespace CrmSuite.Modules.Foundation.Application.Settings.Commands;

public sealed record UpdateSettingCommand(
    string Key,
    string Value,
    string Group = "General",
    bool IsSecret = false) : IRequest<Result<SettingDto>>;
