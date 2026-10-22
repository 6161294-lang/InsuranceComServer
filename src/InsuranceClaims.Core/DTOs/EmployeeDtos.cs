using System.ComponentModel.DataAnnotations;
using InsuranceClaims.Core.Enums;

namespace InsuranceClaims.Core.DTOs;

/// <summary>Creation of an employee by a manager.</summary>
public class EmployeeCreateDto
{
    [Required, StringLength(100, MinimumLength = 2)]
    public string FullName { get; set; } = string.Empty;

    [Required, EmailAddress, StringLength(150)]
    public string Email { get; set; } = string.Empty;

    [Required, StringLength(100, MinimumLength = 8)]
    public string Password { get; set; } = string.Empty;

    [Required, EnumDataType(typeof(EmployeeRole))]
    public EmployeeRole? Role { get; set; }

    /// <summary>Insurance types an appraiser specializes in (ignored for managers).</summary>
    public List<int> SpecializationIds { get; set; } = new();
}

/// <summary>Employee as returned by the API. Never contains the password or its hash.</summary>
public class EmployeeDto
{
    public int Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public EmployeeRole Role { get; set; }
    public List<InsuranceTypeDto> Specializations { get; set; } = new();
}
