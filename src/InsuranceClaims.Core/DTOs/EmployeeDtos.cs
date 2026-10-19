using System.ComponentModel.DataAnnotations;
using InsuranceClaims.Core.Enums;

namespace InsuranceClaims.Core.DTOs;

/// <summary>Self registration. New employees are always created as adjusters.</summary>
public class RegisterDto
{
    [Required, StringLength(100, MinimumLength = 2)]
    public string FullName { get; set; } = string.Empty;

    [Required, StringLength(50, MinimumLength = 3)]
    [RegularExpression("^[a-zA-Z0-9_.-]+$", ErrorMessage = "Username may contain only English letters, digits, dot, dash and underscore.")]
    public string Username { get; set; } = string.Empty;

    [Required, StringLength(100, MinimumLength = 8)]
    public string Password { get; set; } = string.Empty;

    /// <summary>Insurance types the adjuster specializes in.</summary>
    [Required, MinLength(1)]
    public List<int> SpecializationIds { get; set; } = new();
}

/// <summary>Update of an employee by a manager.</summary>
public class EmployeeUpdateDto
{
    [Required, StringLength(100, MinimumLength = 2)]
    public string FullName { get; set; } = string.Empty;

    [Required, EnumDataType(typeof(EmployeeRole))]
    public EmployeeRole? Role { get; set; }

    [Required]
    public List<int> SpecializationIds { get; set; } = new();
}

/// <summary>Employee as returned by the API. Never contains the password or its hash.</summary>
public class EmployeeDto
{
    public int Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public EmployeeRole Role { get; set; }
    public List<InsuranceTypeDto> Specializations { get; set; } = new();
}

public class LoginDto
{
    [Required, StringLength(50)]
    public string Username { get; set; } = string.Empty;

    [Required, StringLength(100)]
    public string Password { get; set; } = string.Empty;
}

public class AuthResponseDto
{
    public string Token { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    public EmployeeDto Employee { get; set; } = new();
}
