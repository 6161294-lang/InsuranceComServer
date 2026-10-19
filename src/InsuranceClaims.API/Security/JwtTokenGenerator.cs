using System.Security.Claims;
using System.Text;
using InsuranceClaims.Core.Common;
using InsuranceClaims.Core.Entities;
using InsuranceClaims.Core.Interfaces.Services;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using SecurityClaim = System.Security.Claims.Claim;

namespace InsuranceClaims.API.Security;

/// <summary>Issues signed JWTs. Stateless, so it is registered as a Singleton.</summary>
public class JwtTokenGenerator : ITokenGenerator
{
    private readonly JwtSettings _settings;
    private readonly TimeProvider _timeProvider;

    public JwtTokenGenerator(IOptions<JwtSettings> settings, TimeProvider timeProvider)
    {
        _settings = settings.Value;
        _timeProvider = timeProvider;
    }

    public (string Token, DateTime ExpiresAt) GenerateToken(Employee employee)
    {
        var now = _timeProvider.GetUtcNow().UtcDateTime;
        var expiresAt = now.AddMinutes(_settings.ExpiryMinutes);

        var claims = new List<SecurityClaim>
        {
            new(AppClaimTypes.EmployeeId, employee.Id.ToString()),
            new(AppClaimTypes.Name, employee.FullName),
            new(AppClaimTypes.Username, employee.Username),
            new(AppClaimTypes.Role, employee.Role.ToString()),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var descriptor = new SecurityTokenDescriptor
        {
            Issuer = _settings.Issuer,
            Audience = _settings.Audience,
            Subject = new ClaimsIdentity(claims),
            IssuedAt = now,
            NotBefore = now,
            Expires = expiresAt,
            SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_settings.Key)),
                SecurityAlgorithms.HmacSha256)
        };

        return (new JsonWebTokenHandler().CreateToken(descriptor), expiresAt);
    }
}
