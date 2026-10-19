using System.ComponentModel.DataAnnotations;
using InsuranceClaims.Core.Enums;

namespace InsuranceClaims.Core.DTOs;

public class ClaimCreateDto
{
    [Range(1, int.MaxValue)]
    public int PolicyId { get; set; }

    [Required, StringLength(150, MinimumLength = 3)]
    public string Title { get; set; } = string.Empty;

    [Required, StringLength(2000, MinimumLength = 3)]
    public string Description { get; set; } = string.Empty;

    [Range(typeof(decimal), "0.01", "100000000")]
    public decimal Amount { get; set; }
}

/// <summary>Update of the claim details (manager only).</summary>
public class ClaimUpdateDto
{
    [Required, StringLength(150, MinimumLength = 3)]
    public string Title { get; set; } = string.Empty;

    [Required, StringLength(2000, MinimumLength = 3)]
    public string Description { get; set; } = string.Empty;

    [Range(typeof(decimal), "0.01", "100000000")]
    public decimal Amount { get; set; }

    /// <summary>The version the client read. A different current version means someone else changed the claim.</summary>
    [Required]
    public Guid? Version { get; set; }
}

/// <summary>
/// Update of the claim handler ("take for handling"). The new handler is the authenticated adjuster.
/// </summary>
public class ClaimHandlerUpdateDto
{
    /// <summary>The version the client read.</summary>
    [Required]
    public Guid? Version { get; set; }
}

/// <summary>Status change by the current handler.</summary>
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
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public ClaimStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }
    public int PolicyId { get; set; }
    public string PolicyNumber { get; set; } = string.Empty;
    public int InsuranceTypeId { get; set; }
    public string InsuranceTypeName { get; set; } = string.Empty;
    public int? HandlerId { get; set; }
    public string? HandlerName { get; set; }
    public DateTime? AssignedAt { get; set; }
    public Guid Version { get; set; }
}

public enum ClaimSortField
{
    CreatedAt,
    Amount,
    Status,
    Title
}

public class ClaimQueryParameters : PagingParameters
{
    public ClaimStatus? Status { get; set; }
    public int? InsuranceTypeId { get; set; }
    public int? HandlerId { get; set; }
    public ClaimSortField SortBy { get; set; } = ClaimSortField.CreatedAt;
    public bool Descending { get; set; } = true;
}
