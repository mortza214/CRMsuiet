namespace CrmSuite.Modules.Foundation.Domain.Enums;

public enum AuditAction
{
    Create = 1,
    Update = 2,
    Delete = 3,
    LoginSucceeded = 4,
    LoginFailed = 5,
    Logout = 6,
    TokenRefreshed = 7,
}
