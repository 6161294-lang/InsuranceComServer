using InsuranceClaims.Core.DTOs;
using InsuranceClaims.Core.Entities;

namespace InsuranceClaims.Core.Interfaces.Repositories;

/// <summary>
/// Commits all changes tracked in the current request (one unit of work).
/// Throws <see cref="Common.ConcurrencyConflictException"/> when an optimistic-concurrency check fails.
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

    /// <summary>Tracked entities, for attaching as specializations.</summary>
    Task<IReadOnlyList<InsuranceType>> GetByIdsAsync(IEnumerable<int> ids, CancellationToken cancellationToken = default);

    Task<bool> NameExistsAsync(string name, int? excludeId = null, CancellationToken cancellationToken = default);
    Task<bool> HasPoliciesAsync(int id, CancellationToken cancellationToken = default);
    void Add(InsuranceType insuranceType);
    void Remove(InsuranceType insuranceType);
}

public interface IPolicyRepository
{
    Task<(IReadOnlyList<Policy> Items, int TotalCount)> GetPagedAsync(PolicyQueryParameters query, CancellationToken cancellationToken = default);

    /// <summary>Read-only (not tracked), includes the insurance type.</summary>
    Task<Policy?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>Tracked entity, for updates.</summary>
    Task<Policy?> GetForUpdateAsync(int id, CancellationToken cancellationToken = default);

    Task<bool> PolicyNumberExistsAsync(string policyNumber, CancellationToken cancellationToken = default);
    Task<bool> HasClaimsAsync(int id, CancellationToken cancellationToken = default);
    void Add(Policy policy);
    void Remove(Policy policy);
}

public interface IClaimRepository
{
    Task<(IReadOnlyList<Claim> Items, int TotalCount)> GetPagedAsync(ClaimQueryParameters query, CancellationToken cancellationToken = default);

    /// <summary>Read-only (not tracked), includes policy, insurance type and handler.</summary>
    Task<Claim?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>Tracked entity including its policy, for updates in the same unit of work.</summary>
    Task<Claim?> GetForUpdateAsync(int id, CancellationToken cancellationToken = default);

    void Add(Claim claim);
    void Remove(Claim claim);
}

public interface IEmployeeRepository
{
    Task<(IReadOnlyList<Employee> Items, int TotalCount)> GetPagedAsync(PagingParameters paging, CancellationToken cancellationToken = default);

    /// <summary>Read-only (not tracked), includes specializations.</summary>
    Task<Employee?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>Tracked entity including specializations, for updates.</summary>
    Task<Employee?> GetForUpdateAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>Read-only (not tracked), includes specializations.</summary>
    Task<Employee?> GetByUsernameAsync(string username, CancellationToken cancellationToken = default);

    Task<bool> UsernameExistsAsync(string username, CancellationToken cancellationToken = default);

    /// <summary>True when the employee currently handles a claim that is still in progress.</summary>
    Task<bool> HasClaimsInProgressAsync(int id, CancellationToken cancellationToken = default);

    void Add(Employee employee);
    void Remove(Employee employee);
}
