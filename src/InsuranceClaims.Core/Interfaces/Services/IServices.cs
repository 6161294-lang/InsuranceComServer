using InsuranceClaims.Core.Common;
using InsuranceClaims.Core.DTOs;

namespace InsuranceClaims.Core.Interfaces.Services;

public interface IInsuranceTypeService
{
    Task<IReadOnlyList<InsuranceTypeDto>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<OperationResult<InsuranceTypeDto>> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>Manager only.</summary>
    Task<OperationResult<InsuranceTypeDto>> CreateAsync(CurrentUser user, InsuranceTypeCreateDto dto, CancellationToken cancellationToken = default);

    /// <summary>Manager only.</summary>
    Task<OperationResult> UpdateAsync(CurrentUser user, int id, InsuranceTypeUpdateDto dto, CancellationToken cancellationToken = default);

    /// <summary>Manager only.</summary>
    Task<OperationResult> DeleteAsync(CurrentUser user, int id, CancellationToken cancellationToken = default);
}

public interface IClientService
{
    /// <summary>Manager: all clients. Appraiser: only clients with an active claim or referral at them (contact details only).</summary>
    Task<OperationResult<PagedResult<ClientDto>>> GetPagedAsync(CurrentUser user, PagingParameters paging, CancellationToken cancellationToken = default);

    /// <summary>The client themself, a manager, or an appraiser with an active claim or referral for that client.</summary>
    Task<OperationResult<ClientDto>> GetByIdAsync(CurrentUser user, int id, CancellationToken cancellationToken = default);

    /// <summary>A client updates only their own details.</summary>
    Task<OperationResult<ClientDto>> UpdateAsync(CurrentUser user, int id, ClientUpdateDto dto, CancellationToken cancellationToken = default);

    /// <summary>Manager only: sets the insurance types a client holds.</summary>
    Task<OperationResult<ClientDto>> UpdateInsuranceTypesAsync(CurrentUser user, int id, ClientInsuranceTypesUpdateDto dto, CancellationToken cancellationToken = default);
}

public interface IClaimService
{
    /// <summary>Client: own claims. Manager: all claims except drafts. Appraiser: own claims plus open claims they may take.</summary>
    Task<OperationResult<PagedResult<ClaimDto>>> GetPagedAsync(CurrentUser user, ClaimQueryParameters query, CancellationToken cancellationToken = default);

    Task<OperationResult<ClaimDto>> GetByIdAsync(CurrentUser user, int id, CancellationToken cancellationToken = default);

    /// <summary>Client only, and only on an insurance type the client holds.</summary>
    Task<OperationResult<ClaimDto>> CreateAsync(CurrentUser user, ClaimCreateDto dto, CancellationToken cancellationToken = default);

    /// <summary>The owning client edits a draft.</summary>
    Task<OperationResult<ClaimDto>> UpdateAsync(CurrentUser user, int id, ClaimUpdateDto dto, CancellationToken cancellationToken = default);

    /// <summary>"Take for handling": the appraiser becomes the handler and a referral is created. Returns Conflict if someone else won.</summary>
    Task<OperationResult<ClaimDto>> TakeAsync(CurrentUser user, int claimId, ClaimHandlerUpdateDto dto, CancellationToken cancellationToken = default);

    /// <summary>Client: submit a draft or cancel. Handling appraiser: approve or reject.</summary>
    Task<OperationResult<ClaimDto>> UpdateStatusAsync(CurrentUser user, int claimId, ClaimStatusUpdateDto dto, CancellationToken cancellationToken = default);

    /// <summary>The owning client deletes a draft.</summary>
    Task<OperationResult> DeleteAsync(CurrentUser user, int id, CancellationToken cancellationToken = default);
}

public interface IReferralService
{
    /// <summary>Appraiser only: the referrals assigned to them.</summary>
    Task<OperationResult<PagedResult<ReferralDto>>> GetPagedAsync(CurrentUser user, PagingParameters paging, CancellationToken cancellationToken = default);

    /// <summary>Only the appraiser the referral belongs to.</summary>
    Task<OperationResult<ReferralDto>> GetByIdAsync(CurrentUser user, int id, CancellationToken cancellationToken = default);

    /// <summary>Only the appraiser the referral belongs to.</summary>
    Task<OperationResult<ReferralDto>> UpdateAsync(CurrentUser user, int id, ReferralUpdateDto dto, CancellationToken cancellationToken = default);
}

public interface IEmployeeService
{
    /// <summary>Manager only.</summary>
    Task<OperationResult<PagedResult<EmployeeDto>>> GetPagedAsync(CurrentUser user, PagingParameters paging, CancellationToken cancellationToken = default);

    /// <summary>Manager only.</summary>
    Task<OperationResult<EmployeeDto>> GetByIdAsync(CurrentUser user, int id, CancellationToken cancellationToken = default);

    /// <summary>Manager only.</summary>
    Task<OperationResult<EmployeeDto>> CreateAsync(CurrentUser user, EmployeeCreateDto dto, CancellationToken cancellationToken = default);

    /// <summary>Manager only. A manager cannot delete themself.</summary>
    Task<OperationResult> DeleteAsync(CurrentUser user, int id, CancellationToken cancellationToken = default);
}

public interface IAuthService
{
    /// <summary>Registers a new client.</summary>
    Task<OperationResult<ClientDto>> RegisterClientAsync(RegisterClientDto dto, CancellationToken cancellationToken = default);

    /// <summary>Logs in a client or an employee by email and password.</summary>
    Task<OperationResult<AuthResponseDto>> LoginAsync(LoginDto dto, CancellationToken cancellationToken = default);
}

public interface IPasswordHasher
{
    string Hash(string password);
    bool Verify(string password, string passwordHash);
}

public interface ITokenGenerator
{
    (string Token, DateTime ExpiresAt) GenerateToken(int userId, string fullName, string email, string role);
}
