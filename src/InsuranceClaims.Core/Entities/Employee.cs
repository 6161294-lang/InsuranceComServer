using InsuranceClaims.Core.Enums;

namespace InsuranceClaims.Core.Entities;

/// <summary>A company employee (manager or appraiser) - a user of the system.</summary>
public class Employee
{
    public int Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;

    /// <summary>BCrypt hash - the plain password is never stored.</summary>
    public string PasswordHash { get; set; } = string.Empty;

    public EmployeeRole Role { get; set; }

    /// <summary>Insurance types this appraiser is allowed to handle (many-to-many).</summary>
    public ICollection<InsuranceType> Specializations { get; set; } = new List<InsuranceType>();

    public ICollection<Claim> HandledClaims { get; set; } = new List<Claim>();
    public ICollection<Referral> Referrals { get; set; } = new List<Referral>();
}
