using InsuranceClaims.Core.Enums;

namespace InsuranceClaims.API.Security;

/// <summary>Role names for [Authorize(Roles = ...)] - identical to the EmployeeRole values written into the token.</summary>
public static class Roles
{
    public const string Manager = nameof(EmployeeRole.Manager);
    public const string Adjuster = nameof(EmployeeRole.Adjuster);
}
