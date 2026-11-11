namespace CrmSuite.Modules.Foundation.Application.Abstractions;

/// <summary>The Foundation module needs the actor and source IP for audit records.</summary>
public interface ICurrentUserService
{
    Guid? UserId { get; }

    string? UserName { get; }

    string? IpAddress { get; }

    bool IsAuthenticated { get; }
}
