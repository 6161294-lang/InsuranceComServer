using InsuranceClaims.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace InsuranceClaims.Data.Configurations;

// Seeded tables start their identity at 1000 so rows created at runtime never collide with seeded ids.
internal static class IdentityDefaults
{
    public const long StartValue = 1000;
}

public class InsuranceTypeConfiguration : IEntityTypeConfiguration<InsuranceType>
{
    public void Configure(EntityTypeBuilder<InsuranceType> builder)
    {
        builder.ToTable("InsuranceTypes");
        builder.HasKey(t => t.Id);
        builder.Property(t => t.Id).HasIdentityOptions(startValue: IdentityDefaults.StartValue);
        builder.Property(t => t.Name).IsRequired().HasMaxLength(100);
        builder.Property(t => t.Description).HasMaxLength(500);
        builder.HasIndex(t => t.Name).IsUnique();
    }
}

public class ClientConfiguration : IEntityTypeConfiguration<Client>
{
    public void Configure(EntityTypeBuilder<Client> builder)
    {
        builder.ToTable("Clients");
        builder.HasKey(c => c.Id);
        builder.Property(c => c.Id).HasIdentityOptions(startValue: IdentityDefaults.StartValue);
        builder.Property(c => c.FullName).IsRequired().HasMaxLength(100);
        builder.Property(c => c.Email).IsRequired().HasMaxLength(150);
        builder.Property(c => c.PasswordHash).IsRequired().HasMaxLength(100);
        builder.Property(c => c.Phone).IsRequired().HasMaxLength(20);
        builder.Property(c => c.Address).IsRequired().HasMaxLength(200);
        builder.Property(c => c.NationalId).IsRequired().HasMaxLength(9);
        builder.HasIndex(c => c.Email).IsUnique();
        builder.HasIndex(c => c.NationalId).IsUnique();

        // Many-to-many: the insurance types a client holds. EF Core creates the join table.
        builder.HasMany(c => c.InsuranceTypes)
            .WithMany(t => t.Clients)
            .UsingEntity<Dictionary<string, object>>(
                SeedData.ClientInsuranceTypesTable,
                right => right.HasOne<InsuranceType>().WithMany().HasForeignKey("InsuranceTypeId").OnDelete(DeleteBehavior.Restrict),
                left => left.HasOne<Client>().WithMany().HasForeignKey("ClientId").OnDelete(DeleteBehavior.Cascade),
                join => join.HasKey("ClientId", "InsuranceTypeId"));
    }
}

public class EmployeeConfiguration : IEntityTypeConfiguration<Employee>
{
    public void Configure(EntityTypeBuilder<Employee> builder)
    {
        builder.ToTable("Employees");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasIdentityOptions(startValue: IdentityDefaults.StartValue);
        builder.Property(e => e.FullName).IsRequired().HasMaxLength(100);
        builder.Property(e => e.Email).IsRequired().HasMaxLength(150);
        builder.Property(e => e.PasswordHash).IsRequired().HasMaxLength(100);
        builder.Property(e => e.Role).HasConversion<string>().HasMaxLength(20);
        builder.HasIndex(e => e.Email).IsUnique();

        // Many-to-many: an appraiser specializes in several insurance types,
        // and each insurance type has several specialists.
        builder.HasMany(e => e.Specializations)
            .WithMany(t => t.Specialists)
            .UsingEntity<Dictionary<string, object>>(
                SeedData.SpecializationsTable,
                right => right.HasOne<InsuranceType>().WithMany().HasForeignKey("InsuranceTypeId").OnDelete(DeleteBehavior.Restrict),
                left => left.HasOne<Employee>().WithMany().HasForeignKey("EmployeeId").OnDelete(DeleteBehavior.Cascade),
                join => join.HasKey("EmployeeId", "InsuranceTypeId"));
    }
}

public class ClaimConfiguration : IEntityTypeConfiguration<Claim>
{
    public void Configure(EntityTypeBuilder<Claim> builder)
    {
        builder.ToTable("Claims");
        builder.HasKey(c => c.Id);
        builder.Property(c => c.Id).HasIdentityOptions(startValue: IdentityDefaults.StartValue);
        builder.Property(c => c.ClaimNumber).IsRequired().HasMaxLength(30);
        builder.Property(c => c.Description).IsRequired().HasMaxLength(2000);
        builder.Property(c => c.Amount).HasPrecision(18, 2);
        builder.Property(c => c.Status).HasConversion<string>().HasMaxLength(20);
        builder.HasIndex(c => c.ClaimNumber).IsUnique();
        builder.HasIndex(c => c.Status);
        builder.HasIndex(c => c.ClientId);

        builder.HasOne(c => c.Client)
            .WithMany(client => client.Claims)
            .HasForeignKey(c => c.ClientId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(c => c.InsuranceType)
            .WithMany(t => t.Claims)
            .HasForeignKey(c => c.InsuranceTypeId)
            .OnDelete(DeleteBehavior.Restrict);

        // Optional: a claim starts without a handler.
        builder.HasOne(c => c.Handler)
            .WithMany(e => e.HandledClaims)
            .HasForeignKey(c => c.HandlerId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class ReferralConfiguration : IEntityTypeConfiguration<Referral>
{
    public void Configure(EntityTypeBuilder<Referral> builder)
    {
        builder.ToTable("Referrals");
        builder.HasKey(r => r.Id);
        builder.Property(r => r.Id).HasIdentityOptions(startValue: IdentityDefaults.StartValue);
        builder.Property(r => r.Notes).HasMaxLength(2000);
        builder.HasIndex(r => r.AppraiserId);

        builder.HasOne(r => r.Claim)
            .WithMany(c => c.Referrals)
            .HasForeignKey(r => r.ClaimId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(r => r.Appraiser)
            .WithMany(e => e.Referrals)
            .HasForeignKey(r => r.AppraiserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
