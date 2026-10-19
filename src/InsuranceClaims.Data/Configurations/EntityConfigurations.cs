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

public class PolicyConfiguration : IEntityTypeConfiguration<Policy>
{
    public void Configure(EntityTypeBuilder<Policy> builder)
    {
        builder.ToTable("Policies");
        builder.HasKey(p => p.Id);
        builder.Property(p => p.Id).HasIdentityOptions(startValue: IdentityDefaults.StartValue);
        builder.Property(p => p.PolicyNumber).IsRequired().HasMaxLength(30);
        builder.Property(p => p.HolderName).IsRequired().HasMaxLength(150);
        builder.Property(p => p.CoverageAmount).HasPrecision(18, 2);
        builder.HasIndex(p => p.PolicyNumber).IsUnique();

        builder.HasOne(p => p.InsuranceType)
            .WithMany(t => t.Policies)
            .HasForeignKey(p => p.InsuranceTypeId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class ClaimConfiguration : IEntityTypeConfiguration<Claim>
{
    public void Configure(EntityTypeBuilder<Claim> builder)
    {
        builder.ToTable("Claims");
        builder.HasKey(c => c.Id);
        builder.Property(c => c.Id).HasIdentityOptions(startValue: IdentityDefaults.StartValue);
        builder.Property(c => c.Title).IsRequired().HasMaxLength(150);
        builder.Property(c => c.Description).IsRequired().HasMaxLength(2000);
        builder.Property(c => c.Amount).HasPrecision(18, 2);
        builder.Property(c => c.Status).HasConversion<string>().HasMaxLength(20);
        builder.HasIndex(c => c.Status);

        builder.HasOne(c => c.Policy)
            .WithMany(p => p.Claims)
            .HasForeignKey(c => c.PolicyId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(c => c.Handler)
            .WithMany(e => e.HandledClaims)
            .HasForeignKey(c => c.HandlerId)
            .OnDelete(DeleteBehavior.SetNull);
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
        builder.Property(e => e.Username).IsRequired().HasMaxLength(50);
        builder.Property(e => e.PasswordHash).IsRequired().HasMaxLength(100);
        builder.Property(e => e.Role).HasConversion<string>().HasMaxLength(20);
        builder.HasIndex(e => e.Username).IsUnique();

        // Many-to-many: an adjuster specializes in several insurance types,
        // and each insurance type has several specialists.
        builder.HasMany(e => e.Specializations)
            .WithMany(t => t.Specialists)
            .UsingEntity<Dictionary<string, object>>(
                SeedData.SpecializationsTable,
                right => right.HasOne<InsuranceType>().WithMany().HasForeignKey("InsuranceTypeId").OnDelete(DeleteBehavior.Cascade),
                left => left.HasOne<Employee>().WithMany().HasForeignKey("EmployeeId").OnDelete(DeleteBehavior.Cascade),
                join => join.HasKey("EmployeeId", "InsuranceTypeId"));
    }
}
