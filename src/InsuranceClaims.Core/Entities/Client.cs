namespace InsuranceClaims.Core.Entities;

/// <summary>A customer of the company - a user of the system with the Client role.</summary>
public class Client
{
    public int Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;

    /// <summary>BCrypt hash - the plain password is never stored.</summary>
    public string PasswordHash { get; set; } = string.Empty;

    public string Phone { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;

    /// <summary>National identification number (sensitive: never logged, hidden from appraisers).</summary>
    public string NationalId { get; set; } = string.Empty;

    /// <summary>Insurance types the client holds (many-to-many). A claim may be opened only on one of these.</summary>
    public ICollection<InsuranceType> InsuranceTypes { get; set; } = new List<InsuranceType>();

    public ICollection<Claim> Claims { get; set; } = new List<Claim>();
}
