using InsuranceClaims.Core.Common;
using InsuranceClaims.Core.DTOs;
using InsuranceClaims.Core.Entities;

namespace InsuranceClaims.Core.Interfaces.Repositories;

/// <summary>
/// Commits all changes tracked in the current request (one unit of work).
/// Throws <see cref="ConcurrencyConflictException"/> when an optimistic-concurrency check fails.
/// </summary>
public interface IUnitOfWork
{
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}

public interface IInsuranceTypeRepository
{
    Task<IReadOnlyList<InsuranceType>> GetAllAsync(CancellationToken cancellationToken = default);

    /// <summary>Read-only (not tracked).</summary>
    Task<InsuranceType?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>Tracked entity, for updates.</summary>
    Task<InsuranceType?> GetForUpdateAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>Tracked entities, for attaching as specializations or held insurance types.</summary>
    Task<IReadOnlyList<InsuranceType>> GetByIdsAsync(IEnumerable<int> ids, CancellationToken cancellationToken = default);

    Task<bool> NameExistsAsync(string name, int? excludeId = null, CancellationToken cancellationToken = default);

    /// <summary>True when a claim, a client or an appraiser still refers to the insurance type.</summary>
    Task<bool> IsInUseAsync(int id, CancellationToken cancellationToken = default);

    void Add(InsuranceType insuranceType);
    void Remove(InsuranceType insuranceType);
}

public interface IClientRepository
{
    /// <summary>
    /// A page of clients (read-only, with insurance types). When <paramref name="appraiserId"/> is set,
    /// only clients that have an active claim or referral at that appraiser are returned.
    /// </summary>
    Task<(IReadOnlyList<Client> Items, int TotalCount)> GetPagedAsync(PagingParameters paging, int? appraiserId, CancellationToken cancellationToken = default);

    /// <summary>Read-only (not tracked), includes insurance types.</summary>
    Task<Client?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>Tracked entity including insurance types, for updates.</summary>
    Task<Client?> GetForUpdateAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>Read-only (not tracked).</summary>
    Task<Client?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);

    Task<bool> EmailExistsAsync(string email, CancellationToken cancellationToken = default);

    Task<bool> NationalIdExistsAsync(string nationalId, CancellationToken cancellationToken = default);

    /// <summary>True when the client has a claim in treatment that the appraiser handles or has a referral for.</summary>
    Task<bool> HasActiveRelationWithAppraiserAsync(int clientId, int appraiserId, CancellationToken cancellationToken = default);

    /// <summary>True when the client holds the given insurance type.</summary>
    Task<bool> HoldsInsuranceTypeAsync(int clientId, int insuranceTypeId, CancellationToken cancellationToken = default);

    void Add(Client client);
}

public interface IClaimRepository
{
    /// <summary>Filtered by the caller's access and by the query, sorted and paged inside the SQL query.</summary>
    Task<(IReadOnlyList<Claim> Items, int TotalCount)> GetPagedAsync(ClaimQueryParameters query, ClaimAccess access, CancellationToken cancellationToken = default);

    /// <summary>Read-only (not tracked), includes client, insurance type and handler.</summary>
    Task<Claim?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>Tracked entity including client, insurance type and handler, for updates in the same unit of work.</summary>
    Task<Claim?> GetForUpdateAsync(int id, CancellationToken cancellationToken = default);

    void Add(Claim claim);
    void Remove(Claim claim);
}

public interface IReferralRepository
{
    /// <summary>The appraiser's referrals, newest first (read-only).</summary>
    Task<(IReadOnlyList<Referral> Items, int TotalCount)> GetPagedByAppraiserAsync(int appraiserId, PagingParameters paging, CancellationToken cancellationToken = default);

    /// <summary>Read-only (not tracked), includes claim, client, insurance type and appraiser.</summary>
    Task<Referral?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>Tracked entity including its relations, for updates.</summary>
    Task<Referral?> GetForUpdateAsync(int id, CancellationToken cancellationToken = default);

    void Add(Referral referral);
}

public interface IEmployeeRepository
{
    Task<(IReadOnlyList<Employee> Items, int TotalCount)> GetPagedAsync(PagingParameters paging, CancellationToken cancellationToken = default);

    /// <summary>Read-only (not tracked), includes specializations.</summary>
    Task<Employee?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>Tracked entity including specializations, for updates and deletes.</summary>
    Task<Employee?> GetForUpdateAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>Read-only (not tracked), includes specializations.</summary>
    Task<Employee?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);

    Task<bool> EmailExistsAsync(string email, CancellationToken cancellationToken = default);

    /// <summary>True when the employee handles claims or has referrals - deleting would break the history.</summary>
    Task<bool> HasWorkHistoryAsync(int id, CancellationToken cancellationToken = default);

    void Add(Employee employee);
    void Remove(Employee employee);
}
