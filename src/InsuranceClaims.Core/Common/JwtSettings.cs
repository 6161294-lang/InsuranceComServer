namespace InsuranceClaims.Core.Common;

/// <summary>JWT configuration, bound from the "Jwt" section via IOptions. The Key lives in User Secrets.</summary>
public class JwtSettings
{
    public const string SectionName = "Jwt";

    public string Issuer { get; set; } = string.Empty;
    public string Audience { get; set; } = string.Empty;
    public string Key { get; set; } = string.Empty;
    public int ExpiryMinutes { get; set; } = 120;
}
