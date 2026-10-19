using InsuranceClaims.Core.Entities;
using InsuranceClaims.Core.Enums;
using Microsoft.EntityFrameworkCore;

namespace InsuranceClaims.Data;

/// <summary>
/// Demo data, applied through migrations (HasData). All dates are UTC.
/// Demo users: manager / Manager123!, adjuster1 / Adjuster123!, adjuster2 / Adjuster123!
/// Both adjusters specialize in car insurance so they can compete for the same claims.
/// </summary>
public static class SeedData
{
    public const string SpecializationsTable = "EmployeeSpecializations";

    // BCrypt hashes of the demo passwords (generated once; BCrypt salts are random,
    // so the hash must be a constant for the migration to stay deterministic).
    private const string ManagerPasswordHash = "$2a$11$3IGB7AFLarhJ/LN33zo6r.OJ.w7HxDXqrLg4Io04kQL6Rjgcb6qzS";
    private const string AdjusterPasswordHash = "$2a$11$PPQUFfUgQaF182X5Eh12KeDNkCkta.TvZKVMvzOy4Nq.ykiI.6A8.";

    private static DateTime Utc(int year, int month, int day, int hour = 0) =>
        new(year, month, day, hour, 0, 0, DateTimeKind.Utc);

    public static void Apply(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<InsuranceType>().HasData(
            new InsuranceType { Id = 1, Name = "ביטוח רכב", Description = "ביטוח מקיף, צד ג' וחובה" },
            new InsuranceType { Id = 2, Name = "ביטוח דירה", Description = "מבנה ותכולה" },
            new InsuranceType { Id = 3, Name = "ביטוח בריאות", Description = "ניתוחים, תרופות ושירותים רפואיים" });

        modelBuilder.Entity<Policy>().HasData(
            new Policy { Id = 1, PolicyNumber = "CAR-1001", HolderName = "ישראל ישראלי", CoverageAmount = 150000m, InsuranceTypeId = 1, StartDate = Utc(2025, 1, 1), EndDate = Utc(2030, 12, 31) },
            new Policy { Id = 2, PolicyNumber = "CAR-1002", HolderName = "דנה כהן", CoverageAmount = 120000m, InsuranceTypeId = 1, StartDate = Utc(2025, 6, 1), EndDate = Utc(2031, 5, 31) },
            new Policy { Id = 3, PolicyNumber = "HOME-2001", HolderName = "משה לוי", CoverageAmount = 900000m, InsuranceTypeId = 2, StartDate = Utc(2025, 1, 1), EndDate = Utc(2030, 12, 31) },
            new Policy { Id = 4, PolicyNumber = "HEALTH-3001", HolderName = "רונית אברהם", CoverageAmount = 500000m, InsuranceTypeId = 3, StartDate = Utc(2025, 1, 1), EndDate = Utc(2030, 12, 31) },
            new Policy { Id = 5, PolicyNumber = "CAR-0999", HolderName = "יוסי מזרחי", CoverageAmount = 80000m, InsuranceTypeId = 1, StartDate = Utc(2022, 1, 1), EndDate = Utc(2023, 12, 31) });

        modelBuilder.Entity<Claim>().HasData(
            NewClaim(1, 1, "תאונה בצומת", "פגיעה בדלת שמאל בצומת עזריאלי", 12500m, Utc(2026, 9, 1, 8)),
            NewClaim(2, 1, "שבר שמשה קדמית", "אבן שהועפה מרכב בכביש 6", 2300m, Utc(2026, 9, 2, 9)),
            NewClaim(3, 2, "פגיעה בחניה", "רכב פגע ברכב החונה ונמלט", 4100m, Utc(2026, 9, 3, 10)),
            NewClaim(4, 2, "גניבת מראות", "שתי מראות צד נגנבו בלילה", 1800m, Utc(2026, 9, 4, 11)),
            NewClaim(5, 1, "נזק מברד", "שקעים בגג ובמכסה המנוע", 7600m, Utc(2026, 9, 5, 12)),
            NewClaim(6, 2, "תאונה בכביש מהיר", "התנגשות מאחור בפקק", 18900m, Utc(2026, 9, 6, 13)),
            NewClaim(7, 3, "נזילה מהתקרה", "נזק מים בסלון מהשכנים מלמעלה", 9500m, Utc(2026, 9, 7, 14)),
            NewClaim(8, 3, "פריצה לדירה", "גניבת מחשב נייד ותכשיטים", 22000m, Utc(2026, 9, 8, 15)),
            NewClaim(9, 4, "ניתוח ברך", "החזר על ניתוח פרטי", 35000m, Utc(2026, 9, 9, 16)),
            NewClaim(10, 4, "תרופה שאינה בסל", "החזר על תרופה לחצי שנה", 6400m, Utc(2026, 9, 10, 17)),
            NewClaim(11, 1, "שריטות בצד ימין", "שריטה לאורך שתי דלתות", 3200m, Utc(2026, 9, 11, 8)),
            NewClaim(12, 2, "פנס אחורי שבור", "פגיעה ברוורס בחניון", 950m, Utc(2026, 9, 12, 9)));

        modelBuilder.Entity<Employee>().HasData(
            new Employee { Id = 1, FullName = "מנהל המערכת", Username = "manager", PasswordHash = ManagerPasswordHash, Role = EmployeeRole.Manager },
            new Employee { Id = 2, FullName = "שמאי ראשון", Username = "adjuster1", PasswordHash = AdjusterPasswordHash, Role = EmployeeRole.Adjuster },
            new Employee { Id = 3, FullName = "שמאי שני", Username = "adjuster2", PasswordHash = AdjusterPasswordHash, Role = EmployeeRole.Adjuster });

        modelBuilder.Entity(SpecializationsTable).HasData(
            new Dictionary<string, object> { ["EmployeeId"] = 2, ["InsuranceTypeId"] = 1 },
            new Dictionary<string, object> { ["EmployeeId"] = 2, ["InsuranceTypeId"] = 2 },
            new Dictionary<string, object> { ["EmployeeId"] = 3, ["InsuranceTypeId"] = 1 },
            new Dictionary<string, object> { ["EmployeeId"] = 3, ["InsuranceTypeId"] = 3 });
    }

    private static Claim NewClaim(int id, int policyId, string title, string description, decimal amount, DateTime createdAt) => new()
    {
        Id = id,
        PolicyId = policyId,
        Title = title,
        Description = description,
        Amount = amount,
        Status = ClaimStatus.Open,
        CreatedAt = createdAt,
        // Seed data must be deterministic, so each seeded claim gets a fixed initial version.
        Version = new Guid($"00000000-0000-0000-0000-{id:D12}")
    };
}
