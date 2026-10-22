namespace InsuranceClaims.Core.Common;

/// <summary>
/// Which claims a caller is allowed to see. Built by the service from the caller's role and
/// applied by the repository inside the query, so filtering and paging stay in SQL.
/// </summary>
/// <param name="ClientId">Set for a client: only that client's claims.</param>
/// <param name="AppraiserId">Set for an appraiser: claims handled by them, plus open claims in their specializations.</param>
/// <param name="SpecializationIds">The appraiser's insurance types.</param>
/// <remarks>When neither id is set (manager) all claims except private drafts are visible.</remarks>
public sealed record ClaimAccess(int? ClientId, int? AppraiserId, IReadOnlyCollection<int> SpecializationIds)
{
    public static ClaimAccess ForClient(int clientId) => new(clientId, null, Array.Empty<int>());
    public static ClaimAccess ForAppraiser(int appraiserId, IReadOnlyCollection<int> specializationIds) => new(null, appraiserId, specializationIds);
    public static ClaimAccess ForManager() => new(null, null, Array.Empty<int>());
}
