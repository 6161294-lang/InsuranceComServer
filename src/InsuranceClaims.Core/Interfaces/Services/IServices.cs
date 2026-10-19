using InsuranceClaims.Core.Common;
using InsuranceClaims.Core.DTOs;
using InsuranceClaims.Core.Entities;

namespace InsuranceClaims.Core.Interfaces.Services;

public interface IInsuranceTypeService
{
    Task<IReadOnlyList<InsuranceTypeDto>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<OperationResult<InsuranceTypeDto>> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<OperationResult<InsuranceTypeDto>> CreateAsync(InsuranceTypeCreateDto dto, CancellationToken cancellationToken = default);
    Task<OperationResult> UpdateAsync(int id, InsuranceTypeUpdateDto dto, CancellationToken cancellationToken = default);
    Task<OperationResult> DeleteAsync(int id, CancellationToken cancellationToken = default);
}

public interface IPolicyService
{
    Task<PagedResult<PolicyDto>> GetPagedAsync(PolicyQueryParameters query, CancellationToken cancellationToken = default);
    Task<OperationResult<PolicyDto>> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<OperationResult<PolicyDto>> CreateAsync(PolicyCreateDto dto, CancellationToken cancellationToken = default);
    Task<OperationResult> UpdateAsync(int id, PolicyUpdateDto dto, CancellationToken cancellationToken = default);
    Task<OperationResult> DeleteAsync(int id, CancellationToken cancellationToken = default);
}

public interface IClaimService
{
    Task<PagedResult<ClaimDto>> GetPagedAsync(ClaimQueryParameters query, CancellationToken cancellationToken = default);
    Task<OperationResult<ClaimDto>> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<OperationResult<ClaimDto>> CreateAsync(ClaimCreateDto dto, CancellationToken cancellationToken = default);
    Task<OperationResult<ClaimDto>> UpdateAsync(int id, ClaimUpdateDto dto, CancellationToken cancellationToken = default);

    /// <summary>"Take for handling": sets the given adjuster as the claim handler. Returns Conflict if someone else won.</summary>
    Task<OperationResult<ClaimDto>> TakeAsync(int claimId, int employeeId, ClaimHandlerUpdateDto dto, CancellationToken cancellationToken = default);

    /// <summary>Status change - allowed only for the current handler.</summary>
    Task<OperationResult<ClaimDto>> UpdateStatusAsync(int claimId, int employeeId, ClaimStatusUpdateDto dto, CancellationToken cancellationToken = default);

    Task<OperationResult> DeleteAsync(int id, CancellationToken cancellationToken = default);
}

public interface IEmployeeService
{
    Task<PagedResult<EmployeeDto>> GetPagedAsync(PagingParameters paging, CancellationToken cancellationToken = default);
    Task<OperationResult<EmployeeDto>> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<OperationResult<EmployeeDto>> UpdateAsync(int id, EmployeeUpdateDto dto, CancellationToken cancellationToken = default);
    Task<OperationResult> DeleteAsync(int id, int currentEmployeeId, CancellationToken cancellationToken = default);
}

public interface IAuthService
{
    /// <summary>Creates a new adjuster (this is the "create" operation of the employees resource).</summary>
    Task<OperationResult<EmployeeDto>> RegisterAsync(RegisterDto dto, CancellationToken cancellationToken = default);
    Task<OperationResult<AuthResponseDto>> LoginAsync(LoginDto dto, CancellationToken cancellationToken = default);
}

public interface IPasswordHasher
{
    string Hash(string password);
    bool Verify(string password, string passwordHash);
}

public interface ITokenGenerator
{
    (string Token, DateTime ExpiresAt) GenerateToken(Employee employee);
}
