using CrmSuite.BuildingBlocks.Application.Abstractions;
using CrmSuite.BuildingBlocks.Domain.Exceptions;
using CrmSuite.BuildingBlocks.Domain.Results;
using CrmSuite.Modules.Foundation.Application.Abstractions;
using CrmSuite.Modules.Workflow.Application.Abstractions;
using CrmSuite.Modules.Workflow.Application.Dtos;
using CrmSuite.Modules.Workflow.Application.Mapping;
using MediatR;
using FoundationCurrentUser = CrmSuite.Modules.Foundation.Application.Abstractions.ICurrentUserService;

namespace CrmSuite.Modules.Workflow.Application.Commands;

public sealed record ReopenWorkflowDefinitionCommand(Guid DefinitionId)
    : IRequest<Result<WorkflowDefinitionDto>>;

public sealed class ReopenWorkflowDefinitionCommandHandler
    : IRequestHandler<ReopenWorkflowDefinitionCommand, Result<WorkflowDefinitionDto>>
{
    private readonly IWorkflowDefinitionRepository _definitions;
    private readonly IWorkflowUnitOfWork _unitOfWork;
    private readonly IAuditScope _auditScope;
    private readonly FoundationCurrentUser _currentUser;
    private readonly IDateTimeProvider _clock;
    private readonly WorkflowDtoMapper _mapper;

    public ReopenWorkflowDefinitionCommandHandler(
        IWorkflowDefinitionRepository definitions,
        IWorkflowUnitOfWork unitOfWork,
        IAuditScope auditScope,
        FoundationCurrentUser currentUser,
        IDateTimeProvider clock,
        WorkflowDtoMapper mapper)
    {
        _definitions = definitions;
        _unitOfWork = unitOfWork;
        _auditScope = auditScope;
        _currentUser = currentUser;
        _clock = clock;
        _mapper = mapper;
    }

    public async Task<Result<WorkflowDefinitionDto>> Handle(
        ReopenWorkflowDefinitionCommand request,
        CancellationToken cancellationToken)
    {
        var definition = await _definitions.GetByIdWithGraphAsync(
            request.DefinitionId,
            cancellationToken);
        if (definition is null)
        {
            return Result.Failure<WorkflowDefinitionDto>(
                "گردش کار یافت نشد.",
                "WORKFLOW_DEFINITION_NOT_FOUND");
        }

        try
        {
            definition.ReopenForEditing(_currentUser.UserId, _clock.UtcNow);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            _auditScope.EntityId = definition.Id.ToString();
            return Result.Success(_mapper.ToDefinition(definition));
        }
        catch (DomainException exception)
        {
            return Result.Failure<WorkflowDefinitionDto>(exception.Message, exception.Code);
        }
    }
}
