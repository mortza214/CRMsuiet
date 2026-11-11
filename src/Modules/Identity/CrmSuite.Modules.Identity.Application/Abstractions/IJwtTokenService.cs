using CrmSuite.Modules.Identity.Application.Auth;
using CrmSuite.Modules.Identity.Domain.Entities;

namespace CrmSuite.Modules.Identity.Application.Abstractions;

public interface IJwtTokenService
{
    TokenPair CreateTokens(
        User user,
        IReadOnlyCollection<string> roles,
        IReadOnlyCollection<string> permissions);
}
