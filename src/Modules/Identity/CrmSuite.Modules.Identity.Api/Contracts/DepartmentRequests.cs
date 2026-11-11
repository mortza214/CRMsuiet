namespace CrmSuite.Modules.Identity.Api.Contracts;

public sealed class CreateDepartmentRequest
{
    public string Name { get; init; } = string.Empty;

    public Guid? ParentId { get; init; }
}

public sealed class UpdateDepartmentRequest
{
    public string Name { get; init; } = string.Empty;

    public Guid? ParentId { get; init; }
}
