using System.Globalization;
using System.Security.Claims;
using Microsoft.IdentityModel.JsonWebTokens;

namespace InsuranceClaims.API.Security;

/// <summary>Short JWT claim names (inbound claim mapping is disabled, so these arrive unchanged).</summary>
public static class AppClaimTypes
{
    public const string EmployeeId = JwtRegisteredClaimNames.Sub;
    public const string Name = JwtRegisteredClaimNames.Name;
    public const string Username = JwtRegisteredClaimNames.UniqueName;
    public const string Role = "role";
}

public static class ClaimsPrincipalExtensions
{
    public static int GetEmployeeId(this ClaimsPrincipal user)
    {
        var value = user.FindFirst(AppClaimTypes.EmployeeId)?.Value;
        return int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var id)
            ? id
            : throw new InvalidOperationException("The authenticated user has no employee id claim.");
    }
}
