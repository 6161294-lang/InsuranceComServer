using System.ComponentModel.DataAnnotations;

namespace InsuranceClaims.Core.DTOs;

/// <summary>Self registration of a client.</summary>
public class RegisterClientDto
{
    [Required, StringLength(100, MinimumLength = 2)]
    public string FullName { get; set; } = string.Empty;

    [Required, EmailAddress, StringLength(150)]
    public string Email { get; set; } = string.Empty;

    [Required, StringLength(100, MinimumLength = 8)]
    public string Password { get; set; } = string.Empty;

    [Required, RegularExpression(@"^[0-9+\-\s]{7,20}$", ErrorMessage = "Invalid phone number.")]
    public string Phone { get; set; } = string.Empty;

    [Required, StringLength(200, MinimumLength = 2)]
    public string Address { get; set; } = string.Empty;

    [Required, RegularExpression(@"^[0-9]{5,9}$", ErrorMessage = "National id must be 5-9 digits.")]
    public string NationalId { get; set; } = string.Empty;

    /// <summary>Insurance types the client holds.</summary>
    [Required, MinLength(1)]
    public List<int> InsuranceTypeIds { get; set; } = new();
}

/// <summary>Update of a client's own details. Email, national id and insurance types are not editable here.</summary>
public class ClientUpdateDto
{
    [Required, StringLength(100, MinimumLength = 2)]
    public string FullName { get; set; } = string.Empty;

    [Required, RegularExpression(@"^[0-9+\-\s]{7,20}$", ErrorMessage = "Invalid phone number.")]
    public string Phone { get; set; } = string.Empty;

    [Required, StringLength(200, MinimumLength = 2)]
    public string Address { get; set; } = string.Empty;
}

/// <summary>Manager updates the insurance types a client holds.</summary>
public class ClientInsuranceTypesUpdateDto
{
    [Required]
    public List<int> InsuranceTypeIds { get; set; } = new();
}

/// <summary>Client as returned by the API. Never contains the password or its hash.</summary>
public class ClientDto
{
    public int Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;

    /// <summary>Hidden (null) when the caller is an appraiser - they only get contact details.</summary>
    public string? NationalId { get; set; }

    public List<InsuranceTypeDto> InsuranceTypes { get; set; } = new();
}
