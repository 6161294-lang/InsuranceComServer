using AutoMapper;
using InsuranceClaims.Core.Common;
using InsuranceClaims.Core.DTOs;
using InsuranceClaims.Core.Entities;
using InsuranceClaims.Core.Enums;
using InsuranceClaims.Core.Interfaces.Repositories;
using InsuranceClaims.Core.Interfaces.Services;
using Microsoft.Extensions.Logging;

namespace InsuranceClaims.Service.Services;

public class ClaimService : IClaimService
{
    private const string ClaimNotFound = "התביעה לא נמצאה";
    private const string ClaimModifiedByAnother = "התביעה עודכנה על ידי משתמש אחר. יש לרענן ולנסות שוב";

    private readonly IClaimRepository _claims;
    private readonly IPolicyRepository _policies;
    private readonly IEmployeeRepository _employees;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<ClaimService> _logger;

    public ClaimService(
        IClaimRepository claims,
        IPolicyRepository policies,
        IEmployeeRepository employees,
        IUnitOfWork unitOfWork,
        IMapper mapper,
        TimeProvider timeProvider,
        ILogger<ClaimService> logger)
    {
        _claims = claims;
        _policies = policies;
        _employees = employees;
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _timeProvider = timeProvider;
        _logger = logger;
    }

    public async Task<PagedResult<ClaimDto>> GetPagedAsync(ClaimQueryParameters query, CancellationToken cancellationToken = default)
    {
        var (items, totalCount) = await _claims.GetPagedAsync(query, cancellationToken);
        return new PagedResult<ClaimDto>
        {
            Items = _mapper.Map<List<ClaimDto>>(items),
            TotalCount = totalCount,
            Page = query.Page,
            PageSize = query.PageSize
        };
    }

    public async Task<OperationResult<ClaimDto>> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var claim = await _claims.GetByIdAsync(id, cancellationToken);
        return claim is null
            ? OperationResult<ClaimDto>.NotFound(ClaimNotFound)
            : OperationResult<ClaimDto>.Success(_mapper.Map<ClaimDto>(claim));
    }

    public async Task<OperationResult<ClaimDto>> CreateAsync(ClaimCreateDto dto, CancellationToken cancellationToken = default)
    {
        var policy = await _policies.GetByIdAsync(dto.PolicyId, cancellationToken);
        if (policy is null)
        {
            return OperationResult<ClaimDto>.ValidationFailed("הפוליסה לא נמצאה");
        }

        var now = _timeProvider.GetUtcNow().UtcDateTime;
        if (!policy.IsActiveAt(now))
        {
            return OperationResult<ClaimDto>.ValidationFailed("לא ניתן לפתוח תביעה על פוליסה שאינה בתוקף");
        }

        var claim = _mapper.Map<Claim>(dto);
        claim.Status = ClaimStatus.Open;
        claim.CreatedAt = now;

        _claims.Add(claim);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Claim {ClaimId} opened on policy {PolicyId}", claim.Id, policy.Id);

        var result = _mapper.Map<ClaimDto>(claim);
        result.PolicyNumber = policy.PolicyNumber;
        result.InsuranceTypeId = policy.InsuranceTypeId;
        result.InsuranceTypeName = policy.InsuranceType?.Name ?? string.Empty;
        return OperationResult<ClaimDto>.Success(result);
    }

    public async Task<OperationResult<ClaimDto>> UpdateAsync(int id, ClaimUpdateDto dto, CancellationToken cancellationToken = default)
    {
        var claim = await _claims.GetForUpdateAsync(id, cancellationToken);
        if (claim is null)
        {
            return OperationResult<ClaimDto>.NotFound(ClaimNotFound);
        }

        if (claim.Version != dto.Version)
        {
            return OperationResult<ClaimDto>.Conflict(ClaimModifiedByAnother);
        }

        _mapper.Map(dto, claim);

        try
        {
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (ConcurrencyConflictException)
        {
            _logger.LogWarning("Concurrency conflict while updating claim {ClaimId}", id);
            return OperationResult<ClaimDto>.Conflict(ClaimModifiedByAnother);
        }

        return OperationResult<ClaimDto>.Success(_mapper.Map<ClaimDto>(claim));
    }

    /// <summary>
    /// "Take for handling". The status check and the save happen in the same unit of work
    /// (the same tracked entity in the same DbContext). If another adjuster saved first,
    /// the Version concurrency token no longer matches and the save fails with a conflict.
    /// </summary>
    public async Task<OperationResult<ClaimDto>> TakeAsync(int claimId, int employeeId, ClaimHandlerUpdateDto dto, CancellationToken cancellationToken = default)
    {
        var claim = await _claims.GetForUpdateAsync(claimId, cancellationToken);
        if (claim is null)
        {
            return OperationResult<ClaimDto>.NotFound(ClaimNotFound);
        }

        if (claim.Status != ClaimStatus.Open || claim.HandlerId is not null)
        {
            _logger.LogWarning("Employee {EmployeeId} tried to take claim {ClaimId} which is already handled by employee {HandlerId}",
                employeeId, claimId, claim.HandlerId);
            return OperationResult<ClaimDto>.Conflict(ErrorMessages.ClaimTakenByAnother);
        }

        var employee = await _employees.GetByIdAsync(employeeId, cancellationToken);
        if (employee is null)
        {
            return OperationResult<ClaimDto>.Unauthorized("העובד המחובר לא נמצא");
        }

        if (employee.Role != EmployeeRole.Adjuster)
        {
            return OperationResult<ClaimDto>.Forbidden("רק שמאי יכול לקחת תביעה לטיפול");
        }

        var insuranceTypeId = claim.Policy?.InsuranceTypeId;
        if (insuranceTypeId is null || employee.Specializations.All(s => s.Id != insuranceTypeId))
        {
            return OperationResult<ClaimDto>.Forbidden("התביעה אינה בתחום ההתמחות שלך");
        }

        if (claim.Version != dto.Version)
        {
            // The client read an older version - someone changed the claim in between.
            _logger.LogWarning("Employee {EmployeeId} tried to take claim {ClaimId} using a stale version", employeeId, claimId);
            return OperationResult<ClaimDto>.Conflict(ErrorMessages.ClaimTakenByAnother);
        }

        claim.HandlerId = employee.Id;
        claim.Status = ClaimStatus.InProgress;
        claim.AssignedAt = _timeProvider.GetUtcNow().UtcDateTime;

        try
        {
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (ConcurrencyConflictException)
        {
            // Lost the race: another adjuster saved between our read and our save.
            _logger.LogWarning("Concurrency conflict: employee {EmployeeId} lost the race for claim {ClaimId}", employeeId, claimId);
            return OperationResult<ClaimDto>.Conflict(ErrorMessages.ClaimTakenByAnother);
        }

        _logger.LogInformation("Claim {ClaimId} taken for handling by employee {EmployeeId}", claimId, employeeId);

        var result = _mapper.Map<ClaimDto>(claim);
        result.HandlerName = employee.FullName;
        return OperationResult<ClaimDto>.Success(result);
    }

    public async Task<OperationResult<ClaimDto>> UpdateStatusAsync(int claimId, int employeeId, ClaimStatusUpdateDto dto, CancellationToken cancellationToken = default)
    {
        var claim = await _claims.GetForUpdateAsync(claimId, cancellationToken);
        if (claim is null)
        {
            return OperationResult<ClaimDto>.NotFound(ClaimNotFound);
        }

        if (claim.HandlerId != employeeId)
        {
            return OperationResult<ClaimDto>.Forbidden("רק העובד המטפל בתביעה רשאי לעדכן את הסטטוס שלה");
        }

        if (claim.Status != ClaimStatus.InProgress)
        {
            return OperationResult<ClaimDto>.ValidationFailed("ניתן לעדכן סטטוס רק לתביעה שנמצאת בטיפול");
        }

        var newStatus = dto.Status!.Value;
        if (newStatus == ClaimStatus.InProgress)
        {
            return OperationResult<ClaimDto>.ValidationFailed("התביעה כבר נמצאת בטיפול");
        }

        if (claim.Version != dto.Version)
        {
            return OperationResult<ClaimDto>.Conflict(ClaimModifiedByAnother);
        }

        if (newStatus == ClaimStatus.Open)
        {
            // Releasing the claim back to the pool.
            claim.Handler = null;
            claim.HandlerId = null;
            claim.AssignedAt = null;
        }

        claim.Status = newStatus;

        try
        {
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (ConcurrencyConflictException)
        {
            _logger.LogWarning("Concurrency conflict while changing status of claim {ClaimId}", claimId);
            return OperationResult<ClaimDto>.Conflict(ClaimModifiedByAnother);
        }

        _logger.LogInformation("Claim {ClaimId} status changed to {Status} by employee {EmployeeId}", claimId, newStatus, employeeId);
        return OperationResult<ClaimDto>.Success(_mapper.Map<ClaimDto>(claim));
    }

    public async Task<OperationResult> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var claim = await _claims.GetForUpdateAsync(id, cancellationToken);
        if (claim is null)
        {
            return OperationResult.NotFound(ClaimNotFound);
        }

        _claims.Remove(claim);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return OperationResult.Success();
    }
}
