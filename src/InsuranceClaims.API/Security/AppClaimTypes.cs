using System.Globalization;
using System.Security.Claims;
using InsuranceClaims.Core.Common;
using Microsoft.IdentityModel.JsonWebTokens;

namespace InsuranceClaims.API.Security;

/// <summary>Short JWT claim names (inbound claim mapping is disabled, so these arrive unchanged).</summary>
public static class AppClaimTypes
{
    public const string UserId = JwtRegisteredClaimNames.Sub;
    public const string Name = JwtRegisteredClaimNames.Name;
    public const string Email = JwtRegisteredClaimNames.Email;
    public const string Role = "role";
}

public static class ClaimsPrincipalExtensions
{
    /// <summary>The caller as the services need it: id and role from the validated token.</summary>
    public static CurrentUser ToCurrentUser(this ClaimsPrincipal user)
    {
        var idValue = user.FindFirst(AppClaimTypes.UserId)?.Value;
        var role = user.FindFirst(AppClaimTypes.Role)?.Value;

        if (!int.TryParse(idValue, NumberStyles.Integer, CultureInfo.InvariantCulture, out var id) || string.IsNullOrEmpty(role))
        {
            throw new InvalidOperationException("The authenticated user has no id or role claim.");
        }

        return new CurrentUser(id, role);
    }
}
