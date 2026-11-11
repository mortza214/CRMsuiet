using CrmSuite.BuildingBlocks.Domain.Results;
using CrmSuite.Modules.Foundation.Application.Abstractions;
using CrmSuite.Modules.Foundation.Application.Dtos;
using MediatR;

namespace CrmSuite.Modules.Foundation.Application.Settings.Commands;

public sealed class UpdateSettingCommandHandler
    : IRequestHandler<UpdateSettingCommand, Result<SettingDto>>
{
    private readonly ISettingService _settingService;
    private readonly IAuditScope _auditScope;

    public UpdateSettingCommandHandler(
        ISettingService settingService,
        IAuditScope auditScope)
    {
        _settingService = settingService;
        _auditScope = auditScope;
    }

    public async Task<Result<SettingDto>> Handle(
        UpdateSettingCommand request,
        CancellationToken cancellationToken)
    {
        // The filter persists the audit row after the action succeeds; the handler supplies
        // the original value so the log contains a real before/after pair.
        _auditScope.OldValues = await _settingService.GetAsync(
            request.Key,
            includeSecret: true,
            cancellationToken);
        _auditScope.EntityId = request.Key;

        return await _settingService.SetAsync(
            request.Key,
            request.Value,
            request.Group,
            request.IsSecret,
            cancellationToken);
    }
}
