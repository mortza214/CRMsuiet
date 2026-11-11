using CrmSuite.BuildingBlocks.Application.Abstractions;
using CrmSuite.BuildingBlocks.Domain.Exceptions;
using CrmSuite.BuildingBlocks.Domain.Results;
using CrmSuite.Modules.Workflow.Application.Abstractions;
using CrmSuite.Modules.Workflow.Application.Dtos;
using CrmSuite.Modules.Workflow.Application.Mapping;
using CrmSuite.Modules.Workflow.Domain.Entities;
using MediatR;
using FoundationCurrentUser = CrmSuite.Modules.Foundation.Application.Abstractions.ICurrentUserService;

namespace CrmSuite.Modules.Workflow.Application.Commands;

public sealed record CloneWorkflowDefinitionCommand(
    Guid SourceDefinitionId,
    string? Code = null) : IRequest<Result<WorkflowDefinitionDto>>;

/// <summary>
/// Creates an editable draft copy of a workflow definition. The original definition
/// and its running instances are not touched.
/// </summary>
public sealed class CloneWorkflowDefinitionCommandHandler
    : IRequestHandler<CloneWorkflowDefinitionCommand, Result<WorkflowDefinitionDto>>
{
    private readonly IWorkflowDefinitionRepository _definitions;
    private readonly IWorkflowUnitOfWork _unitOfWork;
    private readonly FoundationCurrentUser _currentUser;
    private readonly IDateTimeProvider _clock;
    private readonly WorkflowDtoMapper _mapper;

    public CloneWorkflowDefinitionCommandHandler(
        IWorkflowDefinitionRepository definitions,
        IWorkflowUnitOfWork unitOfWork,
        FoundationCurrentUser currentUser,
        IDateTimeProvider clock,
        WorkflowDtoMapper mapper)
    {
        _definitions = definitions;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
        _clock = clock;
        _mapper = mapper;
    }

    public async Task<Result<WorkflowDefinitionDto>> Handle(
        CloneWorkflowDefinitionCommand request,
        CancellationToken cancellationToken)
    {
        var source = await _definitions.GetByIdWithGraphAsync(
            request.SourceDefinitionId,
            cancellationToken);
        if (source is null)
        {
            return Result.Failure<WorkflowDefinitionDto>(
                "گردش کار مبدأ یافت نشد.",
                "WORKFLOW_DEFINITION_NOT_FOUND");
        }

        var now = _clock.UtcNow;
        var code = await BuildUniqueCodeAsync(
            request.Code,
            source.Code,
            cancellationToken);

        try
        {
            var clone = new WorkflowDefinition(
                code,
                $"{source.Name} (کپی)",
                source.EntityType,
                source.Description,
                _currentUser.UserId,
                now);

            var stateMap = new Dictionary<Guid, Guid>();
            foreach (var state in source.States.OrderBy(state => state.Order))
            {
                var clonedState = clone.AddState(
                    state.Code,
                    state.Name,
                    state.StateType,
                    state.Order,
                    state.IsFinal,
                    state.AssigneeType,
                    state.AssigneeValue,
                    state.FormCode,
                    state.Color,
                    state.ActionKey);
                stateMap[state.Id] = clonedState.Id;
            }

            foreach (var transition in source.Transitions.OrderBy(item => item.SortOrder))
            {
                clone.AddTransition(
                    stateMap[transition.FromStateId],
                    stateMap[transition.ToStateId],
                    transition.Name,
                    transition.GuardType,
                    transition.RequiredPermission,
                    transition.GuardPluginKey,
                    transition.RequiresComment,
                    transition.SortOrder,
                    _currentUser.UserId,
                    now);
            }

            _definitions.Add(clone);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            return Result.Success(_mapper.ToDefinition(clone));
        }
        catch (DomainException exception)
        {
            return Result.Failure<WorkflowDefinitionDto>(
                exception.Message,
                exception.Code);
        }
    }

    private async Task<string> BuildUniqueCodeAsync(
        string? requestedCode,
        string sourceCode,
        CancellationToken cancellationToken)
    {
        var baseCode = string.IsNullOrWhiteSpace(requestedCode)
            ? $"{sourceCode}-COPY"
            : requestedCode.Trim().ToUpperInvariant();

        var code = baseCode;
        var suffix = 2;
        while (await _definitions.CodeExistsAsync(code, null, cancellationToken))
        {
            code = $"{baseCode}-{suffix++}";
        }

        return code;
    }
}
