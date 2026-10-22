using InsuranceClaims.Core.Entities;
using InsuranceClaims.Core.Enums;
using Microsoft.EntityFrameworkCore;

namespace InsuranceClaims.Data;

/// <summary>
/// Demo data, applied through migrations (HasData). All dates are UTC.
/// Demo users (password in parentheses):
///   manager@example.com (Manager123!), appraiser1@example.com and appraiser2@example.com (Appraiser123!),
///   client1@example.com and client2@example.com (Client123!).
/// Both appraisers specialize in car insurance but each has a different second specialization,
/// so they compete for open car claims.
/// </summary>
public static class SeedData
{
    public const string SpecializationsTable = "EmployeeSpecializations";
    public const string ClientInsuranceTypesTable = "ClientInsuranceTypes";

    // BCrypt hashes of the demo passwords (generated once; BCrypt salts are random,
    // so the hash must be a constant for the migration to stay deterministic).
    private const string ManagerPasswordHash = "$2a$11$UljqLeoBVjHmKJEV7.472O4QB83DJJt2vPBtnnmv2b92O1mLms6Ha";
    private const string AppraiserPasswordHash = "$2a$11$KL6TTo5sr0QrYwtN8vQVT.poPaW9ey0xXxNbvbUUWq4j0wq.Ntca2";
    private const string ClientPasswordHash = "$2a$11$KJArCNkktCpTLUeP3fXchOwkoRCTlP6KR1hKDhoYfDnjiCYdqqOL.";

    private const int Car = 1;
    private const int Home = 2;
    private const int Health = 3;

    private static DateTime Utc(int year, int month, int day, int hour = 0) =>
        new(year, month, day, hour, 0, 0, DateTimeKind.Utc);

    public static void Apply(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<InsuranceType>().HasData(
            new InsuranceType { Id = Car, Name = "ביטוח רכב", Description = "ביטוח מקיף, צד ג' וחובה" },
            new InsuranceType { Id = Home, Name = "ביטוח דירה", Description = "מבנה ותכולה" },
            new InsuranceType { Id = Health, Name = "ביטוח בריאות", Description = "ניתוחים, תרופות ושירותים רפואיים" });

        modelBuilder.Entity<Employee>().HasData(
            new Employee { Id = 1, FullName = "מנהל המערכת", Email = "manager@example.com", PasswordHash = ManagerPasswordHash, Role = EmployeeRole.Manager },
            new Employee { Id = 2, FullName = "שמאי ראשון", Email = "appraiser1@example.com", PasswordHash = AppraiserPasswordHash, Role = EmployeeRole.Appraiser },
            new Employee { Id = 3, FullName = "שמאי שני", Email = "appraiser2@example.com", PasswordHash = AppraiserPasswordHash, Role = EmployeeRole.Appraiser });

        modelBuilder.Entity(SpecializationsTable).HasData(
            Link("EmployeeId", 2, Car),
            Link("EmployeeId", 2, Home),
            Link("EmployeeId", 3, Car),
            Link("EmployeeId", 3, Health));

        modelBuilder.Entity<Client>().HasData(
            new Client { Id = 1, FullName = "ישראל ישראלי", Email = "client1@example.com", PasswordHash = ClientPasswordHash, Phone = "050-1234567", Address = "הרצל 10, תל אביב", NationalId = "301234567" },
            new Client { Id = 2, FullName = "דנה כהן", Email = "client2@example.com", PasswordHash = ClientPasswordHash, Phone = "052-7654321", Address = "הנשיא 5, חיפה", NationalId = "302345678" });

        // Client 1 holds car and home insurance; client 2 holds car and health insurance.
        modelBuilder.Entity(ClientInsuranceTypesTable).HasData(
            Link("ClientId", 1, Car),
            Link("ClientId", 1, Home),
            Link("ClientId", 2, Car),
            Link("ClientId", 2, Health));

        modelBuilder.Entity<Claim>().HasData(
            NewClaim(1, 1, Car, ClaimStatus.Opened, "תאונה בצומת - פגיעה בדלת שמאל", 12500m, Utc(2026, 9, 1, 8)),
            NewClaim(2, 1, Car, ClaimStatus.Draft, "שבר בשמשה הקדמית (טיוטה)", 2300m, Utc(2026, 9, 2, 9)),
            NewClaim(3, 1, Home, ClaimStatus.Opened, "נזילה מהתקרה - נזק מים בסלון", 9500m, Utc(2026, 9, 3, 10)),
            NewClaim(4, 1, Car, ClaimStatus.InTreatment, "נזק מברד - שקעים בגג ובמכסה המנוע", 7600m, Utc(2026, 9, 4, 11), handlerId: 2, assignedAt: Utc(2026, 9, 5, 9)),
            NewClaim(5, 2, Car, ClaimStatus.Opened, "התנגשות מאחור בכביש מהיר", 18900m, Utc(2026, 9, 6, 13)),
            NewClaim(6, 2, Health, ClaimStatus.Opened, "החזר על ניתוח ברך פרטי", 35000m, Utc(2026, 9, 7, 14)),
            NewClaim(7, 2, Car, ClaimStatus.Approved, "פגיעה בחניה - רכב פגע ונמלט", 4100m, Utc(2026, 9, 8, 15), handlerId: 3, assignedAt: Utc(2026, 9, 9, 9)),
            NewClaim(8, 2, Car, ClaimStatus.Rejected, "גניבת שתי מראות צד", 1800m, Utc(2026, 9, 10, 16), handlerId: 2, assignedAt: Utc(2026, 9, 11, 9)),
            NewClaim(9, 2, Health, ClaimStatus.Cancelled, "החזר על תרופה שאינה בסל", 6400m, Utc(2026, 9, 12, 17)));

        modelBuilder.Entity<Referral>().HasData(
            new Referral { Id = 1, ClaimId = 4, AppraiserId = 2, ReferredAt = Utc(2026, 9, 5, 9), Notes = "נקבעה פגישה לבדיקת הרכב" },
            new Referral { Id = 2, ClaimId = 7, AppraiserId = 3, ReferredAt = Utc(2026, 9, 9, 9), Notes = "הנזק אומת - מומלץ לאשר" },
            new Referral { Id = 3, ClaimId = 8, AppraiserId = 2, ReferredAt = Utc(2026, 9, 11, 9), Notes = "לא נמצא תיעוד לגניבה" });
    }

    private static Dictionary<string, object> Link(string ownerColumn, int ownerId, int insuranceTypeId) =>
        new() { [ownerColumn] = ownerId, ["InsuranceTypeId"] = insuranceTypeId };

    private static Claim NewClaim(int id, int clientId, int insuranceTypeId, ClaimStatus status, string description,
        decimal amount, DateTime openedAt, int? handlerId = null, DateTime? assignedAt = null) => new()
    {
        Id = id,
        ClaimNumber = $"CLM-{id:D4}",
        ClientId = clientId,
        InsuranceTypeId = insuranceTypeId,
        Status = status,
        Description = description,
        Amount = amount,
        OpenedAt = openedAt,
        HandlerId = handlerId,
        AssignedAt = assignedAt,
        // Seed data must be deterministic, so each seeded claim gets a fixed initial version.
        Version = new Guid($"00000000-0000-0000-0000-{id:D12}")
    };
}
