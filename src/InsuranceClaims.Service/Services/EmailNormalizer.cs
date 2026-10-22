namespace InsuranceClaims.Service.Services;

internal static class EmailNormalizer
{
    /// <summary>Emails are stored and compared in lower case, without surrounding spaces.</summary>
    public static string Normalize(string email) => email.Trim().ToLowerInvariant();
}
