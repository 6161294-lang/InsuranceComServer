using InsuranceClaims.Data;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace InsuranceClaims.Tests.Concurrency;

/// <summary>
/// Recreates the dedicated test database once (all migrations + seed) for the concurrency tests.
/// Refuses to touch a database whose name does not contain "test", so the development database is never wiped.
/// </summary>
public sealed class PostgresDatabaseFixture : IAsyncLifetime
{
    private readonly string? _connectionString =
        Environment.GetEnvironmentVariable(PostgresFactAttribute.ConnectionStringVariable);

    public AppDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(_connectionString)
            .Options;
        return new AppDbContext(options);
    }

    public async Task InitializeAsync()
    {
        if (string.IsNullOrWhiteSpace(_connectionString))
        {
            return; // tests are skipped
        }

        var database = new NpgsqlConnectionStringBuilder(_connectionString).Database ?? string.Empty;
        if (!database.Contains("test", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                $"Refusing to recreate database '{database}': the concurrency tests need a separate database whose name contains 'test'.");
        }

        await using var context = CreateContext();
        await context.Database.EnsureDeletedAsync();
        await context.Database.MigrateAsync();
    }

    public Task DisposeAsync() => Task.CompletedTask;
}

[CollectionDefinition(Name)]
public sealed class PostgresCollection : ICollectionFixture<PostgresDatabaseFixture>
{
    public const string Name = "PostgreSQL";
}
