namespace CrmSuite.Modules.Identity.Domain.Entities;

public sealed class UserDepartment
{
    private UserDepartment()
    {
    }

    public UserDepartment(Guid userId, Guid departmentId)
    {
        UserId = userId;
        DepartmentId = departmentId;
    }

    public Guid UserId { get; private set; }

    public User User { get; private set; } = null!;

    public Guid DepartmentId { get; private set; }

    public Department Department { get; private set; } = null!;
}
