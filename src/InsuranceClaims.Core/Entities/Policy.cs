namespace InsuranceClaims.Core.Entities;

public class Policy
{
    public int Id { get; set; }
    public string PolicyNumber { get; set; } = string.Empty;
    public string HolderName { get; set; } = string.Empty;
    public decimal CoverageAmount { get; set; }

    /// <summary>Start of coverage (UTC).</summary>
    public DateTime StartDate { get; set; }

    /// <summary>End of coverage (UTC).</summary>
    public DateTime EndDate { get; set; }

    public int InsuranceTypeId { get; set; }
    public InsuranceType? InsuranceType { get; set; }

    public ICollection<Claim> Claims { get; set; } = new List<Claim>();

    public bool IsActiveAt(DateTime utcNow) => StartDate <= utcNow && utcNow <= EndDate;
}
