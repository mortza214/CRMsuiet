using CrmSuite.BuildingBlocks.Domain.Results;
using CrmSuite.BuildingBlocks.Infrastructure.Persistence;
using CrmSuite.Modules.Forms.Application.Abstractions;
using CrmSuite.Modules.Forms.Application.Dtos;
using CrmSuite.Modules.Forms.Application.Mapping;
using CrmSuite.Modules.Forms.Application.Queries;
using CrmSuite.Modules.Forms.Domain.Entities;
using CrmSuite.Modules.Forms.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace CrmSuite.Modules.Forms.Infrastructure.Persistence.Repositories;

public sealed class FormSubmissionReadRepository : IFormSubmissionReadRepository
{
    private readonly AppDbContext _dbContext;
    private readonly FormDtoMapper _mapper;

    public FormSubmissionReadRepository(AppDbContext dbContext, FormDtoMapper mapper)
    {
        _dbContext = dbContext;
        _mapper = mapper;
    }

    public async Task<PagedResult<FormSubmissionDto>> SearchAsync(
        GetFormSubmissionsQuery query,
        CancellationToken cancellationToken)
    {
        var source =
            from submission in _dbContext.Set<FormSubmission>()
            join template in _dbContext.Set<FormTemplate>()
                on submission.FormTemplateId equals template.Id into templates
            from template in templates.DefaultIfEmpty()
            join customer in _dbContext.Set<CrmSuite.Modules.Customers.Domain.Entities.Customer>()
                on (submission.EntityType == FormEntityType.Customer
                    ? submission.EntityId
                    : submission.CustomerId)
                equals customer.Id into customers
            from customer in customers.DefaultIfEmpty()
            where submission.IsDeleted == false
                  && (!query.FormTemplateId.HasValue || submission.FormTemplateId == query.FormTemplateId)
                  && (!query.CustomerId.HasValue
                      || submission.CustomerId == query.CustomerId
                      || (submission.EntityType == FormEntityType.Customer
                          && submission.EntityId == query.CustomerId))
                  && (!query.EntityType.HasValue || submission.EntityType == query.EntityType)
                  && (!query.EntityId.HasValue || submission.EntityId == query.EntityId)
                  && (!query.Status.HasValue || submission.Status == query.Status)
            select new
            {
                Submission = submission,
                TemplateTitle = template == null ? null : template.Title,
                CustomerName = customer == null
                    ? null
                    : customer.Type == CrmSuite.Modules.Customers.Domain.Enums.CustomerType.Legal
                        ? customer.CompanyName ?? customer.NationalId
                        : ((customer.FirstName ?? string.Empty) + " " + (customer.LastName ?? string.Empty)).Trim(),
            };

        var total = await source.CountAsync(cancellationToken);
        var rows = await source
            .OrderByDescending(row => row.Submission.CreatedAt)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync(cancellationToken);

        var items = rows
            .Select(row => _mapper.ToSubmissionDto(
                row.Submission,
                row.TemplateTitle,
                row.CustomerName))
            .ToArray();

        return new PagedResult<FormSubmissionDto>(items, total, query.Page, query.PageSize);
    }
}
