namespace CrmSuite.Modules.Foundation.Application.Abstractions;

public interface IJalaliDateService
{
    string ToJalali(DateTime utc);

    DateTime ToGregorian(string jalali);

    int GetJalaliYear(DateTime utc);
}
