namespace CrmSuite.IntegrationTests;

/// <summary>Database tests run only when a local MySQL/MariaDB connection is provided.</summary>
public sealed class MySqlFactAttribute : FactAttribute
{
    public MySqlFactAttribute()
    {
        if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("CRMSUITE_TEST_MYSQL")))
        {
            Skip = "Set CRMSUITE_TEST_MYSQL to run database-backed tests.";
        }
    }
}
