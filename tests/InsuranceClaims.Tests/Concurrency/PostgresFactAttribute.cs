namespace InsuranceClaims.Tests.Concurrency;

/// <summary>
/// A [Fact] that runs only when a dedicated PostgreSQL test database is configured
/// in the INSURANCE_CLAIMS_TEST_DB environment variable; otherwise it is reported as skipped.
/// </summary>
public sealed class PostgresFactAttribute : FactAttribute
{
    public const string ConnectionStringVariable = "INSURANCE_CLAIMS_TEST_DB";

    public PostgresFactAttribute()
    {
        if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable(ConnectionStringVariable)))
        {
            Skip = $"Set {ConnectionStringVariable} to a PostgreSQL connection string of a separate test database to run this test.";
        }
    }
}
