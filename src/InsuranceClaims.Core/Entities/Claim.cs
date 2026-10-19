using System.ComponentModel.DataAnnotations;
using InsuranceClaims.Core.Enums;

namespace InsuranceClaims.Core.Entities;

/// <summary>
/// An insurance claim - the limited resource employees compete for.
/// Only one adjuster may take a claim for handling.
/// </summary>
public class Claim
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public ClaimStatus Status { get; set; } = ClaimStatus.Open;

    /// <summary>Creation time (UTC).</summary>
    public DateTime CreatedAt { get; set; }

    public int PolicyId { get; set; }
    public Policy? Policy { get; set; }

    /// <summary>The adjuster currently handling the claim (null while the claim is open).</summary>
    public int? HandlerId { get; set; }
    public Employee? Handler { get; set; }

    /// <summary>When the handler took the claim (UTC).</summary>
    public DateTime? AssignedAt { get; set; }

    /// <summary>
    /// Optimistic-concurrency token. Replaced on every save (see AppDbContext.SaveChangesAsync),
    /// so an UPDATE based on a stale read matches zero rows and fails.
    /// </summary>
    [ConcurrencyCheck]
    public Guid Version { get; set; }
}
