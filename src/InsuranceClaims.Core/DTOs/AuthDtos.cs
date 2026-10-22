using System.ComponentModel.DataAnnotations;

namespace InsuranceClaims.Core.DTOs;

public class LoginDto
{
    [Required, StringLength(150)]
    public string Email { get; set; } = string.Empty;

    [Required, StringLength(100)]
    public string Password { get; set; } = string.Empty;
}

/// <summary>The logged-in user (a client, a manager or an appraiser).</summary>
public class UserDto
{
    public int Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;

    /// <summary>Client, Manager or Appraiser.</summary>
    public string Role { get; set; } = string.Empty;
}

public class AuthResponseDto
{
    public string Token { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    public UserDto User { get; set; } = new();
}
