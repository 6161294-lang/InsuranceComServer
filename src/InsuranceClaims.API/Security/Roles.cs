using InsuranceClaims.Core.Common;

namespace InsuranceClaims.API.Security;

/// <summary>Role names for [Authorize(Roles = ...)] - identical to the role claim written into the token.</summary>
public static class Roles
{
    public const string Client = AppRoles.Client;
    public const string Manager = AppRoles.Manager;
    public const string Appraiser = AppRoles.Appraiser;

    public const string ManagerOrAppraiser = Manager + "," + Appraiser;
    public const string ClientOrAppraiser = Client + "," + Appraiser;
}
