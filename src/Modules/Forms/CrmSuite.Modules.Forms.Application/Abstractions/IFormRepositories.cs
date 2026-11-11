using CrmSuite.Modules.Forms.Domain.Entities;
using CrmSuite.Modules.Forms.Domain.Enums;

namespace CrmSuite.Modules.Forms.Application.Abstractions;

public interface IFormTemplateRepository
{
    Task<FormTemplate?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<FormTemplate?> GetByIdWithFieldsAsync(Guid id, CancellationToken cancellationToken);

    Task<FormTemplate?> GetByCodeAsync(string code, CancellationToken cancellationToken);

    Task<bool> CodeExistsAsync(string code, Guid? excludeId, CancellationToken cancellationToken);

    Task<IReadOnlyList<FormTemplate>> GetAllAsync(bool includeInactive, CancellationToken cancellationToken);

    void Add(FormTemplate formTemplate);
}

public interface IFormSubmissionRepository
{
    Task<FormSubmission?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<FormSubmission?> GetLatestForEntityAsync(
        Guid formDefinitionId,
        FormEntityType entityType,
        Guid entityId,
        CancellationToken cancellationToken);

    Task<bool> ExistsForEntityAsync(
        Guid formDefinitionId,
        FormEntityType entityType,
        Guid entityId,
        CancellationToken cancellationToken);

    void Add(FormSubmission submission);
}

public interface IFormUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}

public interface IFormSchemaValidator
{
    IReadOnlyList<string> Validate(string schemaJson, string submittedDataJson);
}
