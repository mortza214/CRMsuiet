using CrmSuite.BuildingBlocks.Domain.Results;
using CrmSuite.Modules.Identity.Application.Abstractions;
using CrmSuite.Modules.Identity.Application.Dtos;
using CrmSuite.Modules.Identity.Application.Mapping;
using MediatR;

namespace CrmSuite.Modules.Identity.Application.Users.Queries;

public sealed class GetUserByIdQueryHandler : IRequestHandler<GetUserByIdQuery, Result<UserDto>>
{
    private readonly IUserRepository _users;
    private readonly IdentityDtoMapper _mapper;

    public GetUserByIdQueryHandler(IUserRepository users, IdentityDtoMapper mapper)
    {
        _users = users;
        _mapper = mapper;
    }

    public async Task<Result<UserDto>> Handle(
        GetUserByIdQuery request,
        CancellationToken cancellationToken)
    {
        var user = await _users.GetByIdWithAccessAsync(request.Id, cancellationToken);

        return user is null
            ? Result.Failure<UserDto>("کاربر یافت نشد.", "IDENTITY_USER_NOT_FOUND")
            : Result.Success(_mapper.ToUserDto(user));
    }
}
