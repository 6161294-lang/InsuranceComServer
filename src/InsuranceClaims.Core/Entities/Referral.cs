namespace InsuranceClaims.Core.Entities;

/// <summary>Referral of a claim to an appraiser. Created when an appraiser takes a claim for handling.</summary>
public class Referral
{
    public int Id { get; set; }

    public int ClaimId { get; set; }
    public Claim? Claim { get; set; }

    public int AppraiserId { get; set; }
    public Employee? Appraiser { get; set; }

    /// <summary>When the claim was referred (UTC).</summary>
    public DateTime ReferredAt { get; set; }

    /// <summary>The appraiser's assessment notes.</summary>
    public string? Notes { get; set; }
}
