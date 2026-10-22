using InsuranceClaims.Core.Common;
using InsuranceClaims.Core.DTOs;
using InsuranceClaims.Core.Entities;
using InsuranceClaims.Core.Enums;
using InsuranceClaims.Data;
using InsuranceClaims.Data.Repositories;
using InsuranceClaims.Service.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace InsuranceClaims.Tests.Concurrency;

/// <summary>
/// Runs against a real, separate PostgreSQL test database (not the development one).
/// Two DbContexts play two appraisers who read the same claim and both try to take it.
/// Seed: claims 1 and 5 are open car claims; appraisers 2 and 3 both specialize in car insurance.
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
        const int claimId = 1;
        const int firstAppraiser = 2;
        const int secondAppraiser = 3;

        await using var first = _database.CreateContext();
        await using var second = _database.CreateContext();

        // Both read the same claim (same Version).
        var claimSeenByFirst = await first.Claims.SingleAsync(c => c.Id == claimId);
        var claimSeenBySecond = await second.Claims.SingleAsync(c => c.Id == claimId);
        Assert.Equal(claimSeenByFirst.Version, claimSeenBySecond.Version);

        // Both take it.
        claimSeenByFirst.HandlerId = firstAppraiser;
        claimSeenByFirst.Status = ClaimStatus.InTreatment;
        claimSeenBySecond.HandlerId = secondAppraiser;
        claimSeenBySecond.Status = ClaimStatus.InTreatment;

        // The first save wins and gets a new Version.
        await first.SaveChangesAsync();

        // The second save filters on the old Version, matches no row, and fails.
        await Assert.ThrowsAsync<DbUpdateConcurrencyException>(() => second.SaveChangesAsync());

        await using var verify = _database.CreateContext();
        var stored = await verify.Claims.AsNoTracking().SingleAsync(c => c.Id == claimId);
        Assert.Equal(firstAppraiser, stored.HandlerId);
        Assert.Equal(claimSeenByFirst.Version, stored.Version);
    }

    [PostgresFact]
    public async Task UnitOfWork_TranslatesConcurrencyFailure_ToConcurrencyConflictException()
    {
        const int claimId = 5;

        await using var first = _database.CreateContext();
        await using var second = _database.CreateContext();

        var claimSeenByFirst = await first.Claims.SingleAsync(c => c.Id == claimId);
        var claimSeenBySecond = await second.Claims.SingleAsync(c => c.Id == claimId);

        claimSeenByFirst.HandlerId = 2;
        claimSeenByFirst.Status = ClaimStatus.InTreatment;
        claimSeenBySecond.HandlerId = 3;
        claimSeenBySecond.Status = ClaimStatus.InTreatment;

        await new UnitOfWork(first).SaveChangesAsync();

        await Assert.ThrowsAsync<ConcurrencyConflictException>(() => new UnitOfWork(second).SaveChangesAsync());
    }

    [PostgresFact]
    public async Task TwoAppraisersTakingTheSameClaim_OneSucceedsAndTheOtherGetsConflict_AndOnlyOneReferralExists()
    {
        // The test creates its own open car claim, so it does not depend on what the other tests changed.
        int claimId;
        Guid version;
        await using (var setup = _database.CreateContext())
        {
            var claim = new Claim
            {
                ClaimNumber = $"CLM-RACE{Random.Shared.Next(1000, 9999)}",
                ClientId = 1,
                InsuranceTypeId = 1,
                Description = "תביעה לבדיקת תחרות",
                Amount = 1000m,
                Status = ClaimStatus.Opened,
                OpenedAt = DateTime.UtcNow
            };
            setup.Claims.Add(claim);
            await setup.SaveChangesAsync();
            claimId = claim.Id;
            version = claim.Version;
        }

        await using var firstContext = _database.CreateContext();
        await using var secondContext = _database.CreateContext();
        var firstService = CreateService(firstContext);
        var secondService = CreateService(secondContext);

        // Both appraisers send "take for handling" at the same time.
        var gate = new Barrier(2);
        var results = await Task.WhenAll(
            Task.Run(() => TakeTogether(firstService, gate, claimId, 2, version)),
            Task.Run(() => TakeTogether(secondService, gate, claimId, 3, version)));

        Assert.Single(results, r => r.Status == ResultStatus.Success);
        var loser = Assert.Single(results, r => r.Status == ResultStatus.Conflict);
        Assert.Equal(ErrorMessages.ClaimTakenByAnother, loser.Error);

        await using var verify = _database.CreateContext();
        Assert.Equal(1, await verify.Referrals.CountAsync(r => r.ClaimId == claimId));
        var stored = await verify.Claims.AsNoTracking().SingleAsync(c => c.Id == claimId);
        Assert.Equal(ClaimStatus.InTreatment, stored.Status);
        Assert.NotNull(stored.HandlerId);
    }

    private static Task<OperationResult<ClaimDto>> TakeTogether(ClaimService service, Barrier gate, int claimId, int appraiserId, Guid version)
    {
        gate.SignalAndWait();
        return service.TakeAsync(new CurrentUser(appraiserId, AppRoles.Appraiser), claimId, new ClaimHandlerUpdateDto { Version = version });
    }

    private static ClaimService CreateService(AppDbContext context) => new(
        new ClaimRepository(context),
        new ClientRepository(context),
        new InsuranceTypeRepository(context),
        new EmployeeRepository(context),
        new ReferralRepository(context),
        new UnitOfWork(context),
        TestMapper.Create(),
        TimeProvider.System,
        NullLogger<ClaimService>.Instance);
}
