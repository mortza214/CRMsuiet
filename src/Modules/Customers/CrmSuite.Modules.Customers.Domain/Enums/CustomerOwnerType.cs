namespace CrmSuite.Modules.Customers.Domain.Enums;

public enum CustomerOwnerType
{
    CurrentUser = 1,
    CompanyReferred = 2,
    CompanyCustomer = 3,
    FollowUp = 4,
    Company = 5,
    SalesExpert = 6,
}

public enum CustomerNotificationChannel
{
    Sms = 1,
    Bale = 2,
}
