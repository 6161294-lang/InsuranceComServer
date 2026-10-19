using InsuranceClaims.Core.Common;
using InsuranceClaims.Core.Enums;
using InsuranceClaims.Data.Repositories;
using Microsoft.EntityFrameworkCore;

namespace InsuranceClaims.Tests.Concurrency;

/// <summary>
/// Runs against a real, separate PostgreSQL test database (not the development one).
/// Two DbContexts play two adjusters who read the same claim and both try to take it.
/// </summary>
[Collection(PostgresCollection.Name)]
[Trait("Category", "Concurrency")]
public class ClaimConcurrencyTests
{
    private readonly PostgresDatabaseFixture _database;

    public ClaimConcurrencyTests(PostgresDatabaseFixture database)
    {
        _database = database;
    }

    [PostgresFact]
    public async Task SecondContextSavingSameClaim_ThrowsDbUpdateConcurrencyException()
    {
        const int claimId = 1;      // seeded open car claim
        const int firstAdjuster = 2;
        const int secondAdjuster = 3;

        await using var first = _database.CreateContext();
        await using var second = _database.CreateContext();

        // Both read the same claim (same Version).
        var claimSeenByFirst = await first.Claims.SingleAsync(c => c.Id == claimId);
        var claimSeenBySecond = await second.Claims.SingleAsync(c => c.Id == claimId);
        Assert.Equal(claimSeenByFirst.Version, claimSeenBySecond.Version);

        // Both take it.
        claimSeenByFirst.HandlerId = firstAdjuster;
        claimSeenByFirst.Status = ClaimStatus.InProgress;
        claimSeenBySecond.HandlerId = secondAdjuster;
        claimSeenBySecond.Status = ClaimStatus.InProgress;

        // The first save wins and gets a new Version.
        await first.SaveChangesAsync();

        // The second save filters on the old Version, matches no row, and fails.
        await Assert.ThrowsAsync<DbUpdateConcurrencyException>(() => second.SaveChangesAsync());

        await using var verify = _database.CreateContext();
        var stored = await verify.Claims.AsNoTracking().SingleAsync(c => c.Id == claimId);
        Assert.Equal(firstAdjuster, stored.HandlerId);
        Assert.Equal(claimSeenByFirst.Version, stored.Version);
    }

    [PostgresFact]
    public async Task UnitOfWork_TranslatesConcurrencyFailure_ToConcurrencyConflictException()
    {
        const int claimId = 2;

        await using var first = _database.CreateContext();
        await using var second = _database.CreateContext();

        var claimSeenByFirst = await first.Claims.SingleAsync(c => c.Id == claimId);
        var claimSeenBySecond = await second.Claims.SingleAsync(c => c.Id == claimId);

        claimSeenByFirst.HandlerId = 2;
        claimSeenByFirst.Status = ClaimStatus.InProgress;
        claimSeenBySecond.HandlerId = 3;
        claimSeenBySecond.Status = ClaimStatus.InProgress;

        await new UnitOfWork(first).SaveChangesAsync();

        await Assert.ThrowsAsync<ConcurrencyConflictException>(() => new UnitOfWork(second).SaveChangesAsync());
    }
}
