using InsuranceClaims.Core.Enums;

namespace InsuranceClaims.Core.Common;

/// <summary>Role names written into the JWT. Employees use the <see cref="EmployeeRole"/> names; clients have their own role.</summary>
public static class AppRoles
{
    public const string Client = "Client";
    public const string Manager = nameof(EmployeeRole.Manager);
    public const string Appraiser = nameof(EmployeeRole.Appraiser);
}

/// <summary>The authenticated caller, as the services need to know it (id + role from the token).</summary>
public sealed record CurrentUser(int Id, string Role)
{
    public bool IsClient => Role == AppRoles.Client;
    public bool IsManager => Role == AppRoles.Manager;
    public bool IsAppraiser => Role == AppRoles.Appraiser;
}
