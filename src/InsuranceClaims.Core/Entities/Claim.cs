using System.ComponentModel.DataAnnotations;
using InsuranceClaims.Core.Enums;

namespace InsuranceClaims.Core.Entities;

/// <summary>
/// An insurance claim - the limited resource appraisers compete for.
/// Only one appraiser may take a claim for handling.
/// </summary>
public class Claim
{
    public int Id { get; set; }

    /// <summary>Human readable claim number, unique.</summary>
    public string ClaimNumber { get; set; } = string.Empty;

    public int ClientId { get; set; }
    public Client? Client { get; set; }

    public int InsuranceTypeId { get; set; }
    public InsuranceType? InsuranceType { get; set; }

    public string Description { get; set; } = string.Empty;
    public decimal Amount { get; set; }

    /// <summary>When the claim was created (UTC).</summary>
    public DateTime OpenedAt { get; set; }

    public ClaimStatus Status { get; set; } = ClaimStatus.Draft;

    /// <summary>The appraiser handling the claim (null until someone takes it).</summary>
    public int? HandlerId { get; set; }
    public Employee? Handler { get; set; }

    /// <summary>When the handler took the claim (UTC).</summary>
    public DateTime? AssignedAt { get; set; }

    public ICollection<Referral> Referrals { get; set; } = new List<Referral>();

    /// <summary>
    /// Optimistic-concurrency token. Replaced on every save (see AppDbContext.SaveChangesAsync),
    /// so an UPDATE based on a stale read matches zero rows and fails.
    /// </summary>
    [ConcurrencyCheck]
    public Guid Version { get; set; }
}
