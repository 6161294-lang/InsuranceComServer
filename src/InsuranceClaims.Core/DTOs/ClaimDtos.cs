using System.ComponentModel.DataAnnotations;
using InsuranceClaims.Core.Enums;

namespace InsuranceClaims.Core.DTOs;

/// <summary>Opens a claim (client only). A draft is saved privately; otherwise the claim is opened right away.</summary>
public class ClaimCreateDto
{
    [Range(1, int.MaxValue)]
    public int InsuranceTypeId { get; set; }

    [Required, StringLength(2000, MinimumLength = 3)]
    public string Description { get; set; } = string.Empty;

    [Range(typeof(decimal), "0.01", "100000000")]
    public decimal Amount { get; set; }

    public bool SaveAsDraft { get; set; }
}

/// <summary>Update of a draft claim's details (the owning client only).</summary>
public class ClaimUpdateDto
{
    [Range(1, int.MaxValue)]
    public int InsuranceTypeId { get; set; }

    [Required, StringLength(2000, MinimumLength = 3)]
    public string Description { get; set; } = string.Empty;

    [Range(typeof(decimal), "0.01", "100000000")]
    public decimal Amount { get; set; }

    /// <summary>The version the client read. A different current version means someone else changed the claim.</summary>
    [Required]
    public Guid? Version { get; set; }
}

/// <summary>
/// Update of the claim handler ("take for handling"). The new handler is the authenticated appraiser.
/// </summary>
public class ClaimHandlerUpdateDto
{
    /// <summary>The version the client read.</summary>
    [Required]
    public Guid? Version { get; set; }
}

/// <summary>Status change: submit a draft, cancel (client) or approve / reject (handling appraiser).</summary>
public class ClaimStatusUpdateDto
{
    [Required, EnumDataType(typeof(ClaimStatus))]
    public ClaimStatus? Status { get; set; }

    /// <summary>The version the client read.</summary>
    [Required]
    public Guid? Version { get; set; }
}

public class ClaimDto
{
    public int Id { get; set; }
    public string ClaimNumber { get; set; } = string.Empty;
    public int ClientId { get; set; }
    public string ClientName { get; set; } = string.Empty;
    public int InsuranceTypeId { get; set; }
    public string InsuranceTypeName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public DateTime OpenedAt { get; set; }
    public ClaimStatus Status { get; set; }
    public int? HandlerId { get; set; }
    public string? HandlerName { get; set; }
    public DateTime? AssignedAt { get; set; }
    public Guid Version { get; set; }
}

public enum ClaimSortField
{
    OpenedAt,
    Amount,
    Status
}

/// <summary>Which part of the visible claims to list (appraisers: their own claims, or the open claims they may take).</summary>
public enum ClaimScope
{
    All,
    Mine,
    Available
}

public class ClaimQueryParameters : PagingParameters
{
    public ClaimStatus? Status { get; set; }
    public int? InsuranceTypeId { get; set; }
    public ClaimScope Scope { get; set; } = ClaimScope.All;
    public ClaimSortField SortBy { get; set; } = ClaimSortField.OpenedAt;
    public bool Descending { get; set; } = true;
}
