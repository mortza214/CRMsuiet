using CrmSuite.BuildingBlocks.Domain.Results;
using CrmSuite.Modules.Identity.Application.Abstractions;
using CrmSuite.Modules.Identity.Application.Dtos;
using CrmSuite.Modules.Identity.Application.Mapping;
using MediatR;

namespace CrmSuite.Modules.Identity.Application.Users.Queries;

public sealed class GetUsersQueryHandler
    : IRequestHandler<GetUsersQuery, Result<PagedResult<UserDto>>>
{
    private readonly IUserRepository _users;
    private readonly IdentityDtoMapper _mapper;

    public GetUsersQueryHandler(IUserRepository users, IdentityDtoMapper mapper)
    {
        _users = users;
        _mapper = mapper;
    }

    public async Task<Result<PagedResult<UserDto>>> Handle(
        GetUsersQuery request,
        CancellationToken cancellationToken)
    {
        var (items, totalCount) = await _users.GetPagedAsync(
            request.Page,
            request.PageSize,
            request.Search,
            request.IsActive,
            cancellationToken);

        var page = new PagedResult<UserDto>(
            items.Select(_mapper.ToUserDto).ToArray(),
            totalCount,
            request.Page,
            request.PageSize);

        return Result.Success(page);
    }
}
