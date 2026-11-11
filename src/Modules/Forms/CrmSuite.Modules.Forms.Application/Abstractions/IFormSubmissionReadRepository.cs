using CrmSuite.BuildingBlocks.Domain.Results;
using CrmSuite.Modules.Forms.Application.Dtos;
using CrmSuite.Modules.Forms.Application.Queries;

namespace CrmSuite.Modules.Forms.Application.Abstractions;

public interface IFormSubmissionReadRepository
{
    Task<PagedResult<FormSubmissionDto>> SearchAsync(
        GetFormSubmissionsQuery query,
        CancellationToken cancellationToken);
}
