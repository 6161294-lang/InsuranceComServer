using InsuranceClaims.Core.Entities;
using Microsoft.EntityFrameworkCore;

namespace InsuranceClaims.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<InsuranceType> InsuranceTypes => Set<InsuranceType>();
    public DbSet<Policy> Policies => Set<Policy>();
    public DbSet<Claim> Claims => Set<Claim>();
    public DbSet<Employee> Employees => Set<Employee>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
        SeedData.Apply(modelBuilder);
    }

    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        RenewClaimVersions();
        return base.SaveChanges(acceptAllChangesOnSuccess);
    }

    public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
    {
        RenewClaimVersions();
        return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }

    /// <summary>
    /// Gives every new or changed claim a fresh Version, so no service has to remember to do it.
    /// EF Core sends the Version that was originally read in the UPDATE's WHERE clause ([ConcurrencyCheck]);
    /// if another request saved first, zero rows match and DbUpdateConcurrencyException is thrown.
    /// </summary>
    private void RenewClaimVersions()
    {
        foreach (var entry in ChangeTracker.Entries<Claim>())
        {
            if (entry.State is EntityState.Added or EntityState.Modified)
            {
                entry.Entity.Version = Guid.NewGuid();
            }
        }
    }
}
