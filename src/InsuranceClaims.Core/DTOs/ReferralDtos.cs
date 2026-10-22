using System.ComponentModel.DataAnnotations;
using InsuranceClaims.Core.Enums;

namespace InsuranceClaims.Core.DTOs;

/// <summary>Update of a referral by its appraiser: the assessment notes.</summary>
public class ReferralUpdateDto
{
    [StringLength(2000)]
    public string? Notes { get; set; }
}

public class ReferralDto
{
    public int Id { get; set; }
    public int ClaimId { get; set; }
    public string ClaimNumber { get; set; } = string.Empty;
    public ClaimStatus ClaimStatus { get; set; }
    public string ClaimDescription { get; set; } = string.Empty;
    public decimal ClaimAmount { get; set; }
    public int ClientId { get; set; }
    public string ClientName { get; set; } = string.Empty;
    public string InsuranceTypeName { get; set; } = string.Empty;
    public int AppraiserId { get; set; }
    public string AppraiserName { get; set; } = string.Empty;
    public DateTime ReferredAt { get; set; }
    public string? Notes { get; set; }
}
