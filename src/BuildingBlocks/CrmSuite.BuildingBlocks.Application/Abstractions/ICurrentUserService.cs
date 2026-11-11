namespace CrmSuite.BuildingBlocks.Application.Abstractions;

public interface ICurrentUserService
{
    Guid? UserId { get; }

    Guid? CustomerId { get; }

    string? UserName { get; }

    bool IsAuthenticated { get; }

    IReadOnlyCollection<string> Permissions { get; }

    bool HasPermission(string permission);
}
