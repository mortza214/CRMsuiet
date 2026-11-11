using CrmSuite.BuildingBlocks.Infrastructure.Persistence;
using CrmSuite.Modules.Forms.Application.Abstractions;
using CrmSuite.Modules.Forms.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace CrmSuite.Modules.Forms.Infrastructure.Persistence.Repositories;

public sealed class FormSubmissionRepository : IFormSubmissionRepository
{
    private readonly AppDbContext _dbContext;

    public FormSubmissionRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<FormSubmission?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken) =>
        _dbContext.Set<FormSubmission>()
            .SingleOrDefaultAsync(submission => submission.Id == id, cancellationToken);

    public Task<FormSubmission?> GetLatestForEntityAsync(
        Guid formDefinitionId,
        CrmSuite.Modules.Forms.Domain.Enums.FormEntityType entityType,
        Guid entityId,
        CancellationToken cancellationToken) =>
        _dbContext.Set<FormSubmission>()
            .Where(submission => submission.FormTemplateId == formDefinitionId
                                 && submission.EntityType == entityType
                                 && submission.EntityId == entityId)
            .OrderByDescending(submission => submission.SubmittedAt)
            .FirstOrDefaultAsync(cancellationToken);

    public Task<bool> ExistsForEntityAsync(
        Guid formDefinitionId,
        CrmSuite.Modules.Forms.Domain.Enums.FormEntityType entityType,
        Guid entityId,
        CancellationToken cancellationToken) =>
        _dbContext.Set<FormSubmission>()
            .AnyAsync(
                submission => submission.FormTemplateId == formDefinitionId
                              && submission.EntityType == entityType
                              && submission.EntityId == entityId
                              && submission.Status != CrmSuite.Modules.Forms.Domain.Enums.FormSubmissionStatus.Rejected,
                cancellationToken);

    public void Add(FormSubmission submission) => _dbContext.Set<FormSubmission>().Add(submission);

    public void AddHistory(FormSubmissionHistory history) =>
        _dbContext.Set<FormSubmissionHistory>().Add(history);
}
