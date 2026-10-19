using InsuranceClaims.Core.Interfaces.Services;

namespace InsuranceClaims.Service.Security;

/// <summary>Salted, slow BCrypt hashing - the plain password is never stored.</summary>
public class BCryptPasswordHasher : IPasswordHasher
{
    private const int WorkFactor = 11;

    public string Hash(string password) => BCrypt.Net.BCrypt.HashPassword(password, WorkFactor);

    public bool Verify(string password, string passwordHash) => BCrypt.Net.BCrypt.Verify(password, passwordHash);
}
