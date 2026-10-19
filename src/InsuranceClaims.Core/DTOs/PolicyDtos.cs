using System.ComponentModel.DataAnnotations;

namespace InsuranceClaims.Core.DTOs;

public class PolicyCreateDto
{
    [Required, StringLength(30, MinimumLength = 3)]
    public string PolicyNumber { get; set; } = string.Empty;

    [Required, StringLength(150, MinimumLength = 2)]
    public string HolderName { get; set; } = string.Empty;

    [Range(typeof(decimal), "0.01", "100000000")]
    public decimal CoverageAmount { get; set; }

    [Required]
    public DateTime? StartDate { get; set; }

    [Required]
    public DateTime? EndDate { get; set; }

    [Range(1, int.MaxValue)]
    public int InsuranceTypeId { get; set; }
}

public class PolicyUpdateDto
{
    [Required, StringLength(150, MinimumLength = 2)]
    public string HolderName { get; set; } = string.Empty;

    [Range(typeof(decimal), "0.01", "100000000")]
    public decimal CoverageAmount { get; set; }

    [Required]
    public DateTime? StartDate { get; set; }

    [Required]
    public DateTime? EndDate { get; set; }
}

public class PolicyDto
{
    public int Id { get; set; }
    public string PolicyNumber { get; set; } = string.Empty;
    public string HolderName { get; set; } = string.Empty;
    public decimal CoverageAmount { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public int InsuranceTypeId { get; set; }
    public string InsuranceTypeName { get; set; } = string.Empty;
}

public class PolicyQueryParameters : PagingParameters
{
    public int? InsuranceTypeId { get; set; }

    /// <summary>Free text search on policy number or holder name.</summary>
    [StringLength(100)]
    public string? Search { get; set; }
}
