using CrmSuite.BuildingBlocks.Infrastructure.Persistence;
using CrmSuite.Modules.Forms.Application.Abstractions;
using CrmSuite.Modules.Forms.Domain.Entities;
using CrmSuite.Modules.Forms.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace CrmSuite.Modules.Forms.Infrastructure.Persistence.Repositories;

public sealed class FormTemplateRepository : IFormTemplateRepository
{
    private readonly AppDbContext _dbContext;

    public FormTemplateRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<FormTemplate?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        _dbContext.Set<FormTemplate>()
            .SingleOrDefaultAsync(template => template.Id == id, cancellationToken);

    public Task<FormTemplate?> GetByIdWithFieldsAsync(
        Guid id,
        CancellationToken cancellationToken) =>
        _dbContext.Set<FormTemplate>()
            .Include(template => template.Fields)
            .SingleOrDefaultAsync(template => template.Id == id, cancellationToken);

    public Task<FormTemplate?> GetByCodeAsync(
        string code,
        CancellationToken cancellationToken)
    {
        var normalized = code.Trim().ToUpperInvariant();
        return _dbContext.Set<FormTemplate>()
            .Include(template => template.Fields)
            .SingleOrDefaultAsync(template => template.Code == normalized, cancellationToken);
    }

    public Task<bool> CodeExistsAsync(
        string code,
        Guid? excludeId,
        CancellationToken cancellationToken)
    {
        var normalized = code.Trim().ToUpperInvariant();
        var query = _dbContext.Set<FormTemplate>()
            .Where(template => template.Code == normalized);

        if (excludeId.HasValue)
        {
            query = query.Where(template => template.Id != excludeId.Value);
        }

        return query.AnyAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<FormTemplate>> GetAllAsync(
        bool includeInactive,
        CancellationToken cancellationToken) =>
        await _dbContext.Set<FormTemplate>()
            .Include(template => template.Fields)
            .Where(template => includeInactive || template.Status == FormDefinitionStatus.Published)
            .OrderByDescending(template => template.CreatedAt)
            .ToListAsync(cancellationToken);

    public void Add(FormTemplate formTemplate) =>
        _dbContext.Set<FormTemplate>().Add(formTemplate);
}
