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
    private readonly IClientRepository _clients;
    private readonly IInsuranceTypeRepository _insuranceTypes;
    private readonly IEmployeeRepository _employees;
    private readonly IReferralRepository _referrals;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<ClaimService> _logger;

    public ClaimService(
        IClaimRepository claims,
        IClientRepository clients,
        IInsuranceTypeRepository insuranceTypes,
        IEmployeeRepository employees,
        IReferralRepository referrals,
        IUnitOfWork unitOfWork,
        IMapper mapper,
        TimeProvider timeProvider,
        ILogger<ClaimService> logger)
    {
        _claims = claims;
        _clients = clients;
        _insuranceTypes = insuranceTypes;
        _employees = employees;
        _referrals = referrals;
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _timeProvider = timeProvider;
        _logger = logger;
    }

    public async Task<OperationResult<PagedResult<ClaimDto>>> GetPagedAsync(CurrentUser user, ClaimQueryParameters query, CancellationToken cancellationToken = default)
    {
        ClaimAccess access;
        if (user.IsClient)
        {
            access = ClaimAccess.ForClient(user.Id);
        }
        else if (user.IsManager)
        {
            access = ClaimAccess.ForManager();
        }
        else if (user.IsAppraiser)
        {
            var appraiser = await _employees.GetByIdAsync(user.Id, cancellationToken);
            if (appraiser is null)
            {
                return OperationResult<PagedResult<ClaimDto>>.Unauthorized("העובד המחובר לא נמצא");
            }

            access = ClaimAccess.ForAppraiser(appraiser.Id, appraiser.Specializations.Select(s => s.Id).ToList());
        }
        else
        {
            return OperationResult<PagedResult<ClaimDto>>.Forbidden("אין הרשאה לצפות בתביעות");
        }

        var (items, totalCount) = await _claims.GetPagedAsync(query, access, cancellationToken);
        return OperationResult<PagedResult<ClaimDto>>.Success(new PagedResult<ClaimDto>
        {
            Items = _mapper.Map<List<ClaimDto>>(items),
            TotalCount = totalCount,
            Page = query.Page,
            PageSize = query.PageSize
        });
    }

    public async Task<OperationResult<ClaimDto>> GetByIdAsync(CurrentUser user, int id, CancellationToken cancellationToken = default)
    {
        var claim = await _claims.GetByIdAsync(id, cancellationToken);
        if (claim is null || !await CanViewAsync(user, claim, cancellationToken))
        {
            // A claim the caller may not see looks exactly like a claim that does not exist.
            return OperationResult<ClaimDto>.NotFound(ClaimNotFound);
        }

        return OperationResult<ClaimDto>.Success(_mapper.Map<ClaimDto>(claim));
    }

    public async Task<OperationResult<ClaimDto>> CreateAsync(CurrentUser user, ClaimCreateDto dto, CancellationToken cancellationToken = default)
    {
        if (!user.IsClient)
        {
            return OperationResult<ClaimDto>.Forbidden("רק לקוח יכול לפתוח תביעה");
        }

        var client = await _clients.GetByIdAsync(user.Id, cancellationToken);
        if (client is null)
        {
            return OperationResult<ClaimDto>.Unauthorized("הלקוח המחובר לא נמצא");
        }

        var insuranceType = client.InsuranceTypes.FirstOrDefault(t => t.Id == dto.InsuranceTypeId);
        if (insuranceType is null)
        {
            return OperationResult<ClaimDto>.ValidationFailed("לא ניתן לפתוח תביעה על סוג ביטוח שהלקוח אינו מחזיק בו");
        }

        var claim = _mapper.Map<Claim>(dto);
        claim.ClaimNumber = $"CLM-{Guid.NewGuid().ToString("N")[..8].ToUpperInvariant()}";
        claim.ClientId = client.Id;
        claim.Status = dto.SaveAsDraft ? ClaimStatus.Draft : ClaimStatus.Opened;
        claim.OpenedAt = _timeProvider.GetUtcNow().UtcDateTime;

        _claims.Add(claim);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Claim {ClaimId} created with status {Status}", claim.Id, claim.Status);

        var result = _mapper.Map<ClaimDto>(claim);
        result.ClientName = client.FullName;
        result.InsuranceTypeName = insuranceType.Name;
        return OperationResult<ClaimDto>.Success(result);
    }

    public async Task<OperationResult<ClaimDto>> UpdateAsync(CurrentUser user, int id, ClaimUpdateDto dto, CancellationToken cancellationToken = default)
    {
        var claim = await _claims.GetForUpdateAsync(id, cancellationToken);
        if (claim is null)
        {
            return OperationResult<ClaimDto>.NotFound(ClaimNotFound);
        }

        if (!user.IsClient || claim.ClientId != user.Id)
        {
            return OperationResult<ClaimDto>.Forbidden("ניתן לערוך רק תביעה של הלקוח עצמו");
        }

        if (claim.Status != ClaimStatus.Draft)
        {
            return OperationResult<ClaimDto>.ValidationFailed("ניתן לערוך רק טיוטה");
        }

        if (!await _clients.HoldsInsuranceTypeAsync(user.Id, dto.InsuranceTypeId, cancellationToken))
        {
            return OperationResult<ClaimDto>.ValidationFailed("לא ניתן לפתוח תביעה על סוג ביטוח שהלקוח אינו מחזיק בו");
        }

        if (claim.Version != dto.Version)
        {
            return OperationResult<ClaimDto>.Conflict(ClaimModifiedByAnother);
        }

        var insuranceType = (await _insuranceTypes.GetByIdsAsync(new[] { dto.InsuranceTypeId }, cancellationToken)).Single();
        _mapper.Map(dto, claim);
        claim.InsuranceType = insuranceType;

        var conflict = await SaveOrConflictAsync(claim.Id, ClaimModifiedByAnother, cancellationToken);
        return conflict ?? OperationResult<ClaimDto>.Success(_mapper.Map<ClaimDto>(claim));
    }

    /// <summary>
    /// "Take for handling". The status check, the claim update and the referral are saved in the same unit of work
    /// (the same tracked entity in the same DbContext). If another appraiser saved first,
    /// the Version concurrency token no longer matches and the whole save fails with a conflict.
    /// </summary>
    public async Task<OperationResult<ClaimDto>> TakeAsync(CurrentUser user, int claimId, ClaimHandlerUpdateDto dto, CancellationToken cancellationToken = default)
    {
        if (!user.IsAppraiser)
        {
            return OperationResult<ClaimDto>.Forbidden("רק שמאי יכול לקחת תביעה לטיפול");
        }

        var claim = await _claims.GetForUpdateAsync(claimId, cancellationToken);
        if (claim is null || claim.Status == ClaimStatus.Draft)
        {
            return OperationResult<ClaimDto>.NotFound(ClaimNotFound);
        }

        if (claim.Status == ClaimStatus.InTreatment || claim.HandlerId is not null)
        {
            _logger.LogWarning("Appraiser {AppraiserId} tried to take claim {ClaimId} which is already handled by appraiser {HandlerId}",
                user.Id, claimId, claim.HandlerId);
            return OperationResult<ClaimDto>.Conflict(ErrorMessages.ClaimTakenByAnother);
        }

        if (claim.Status != ClaimStatus.Opened)
        {
            return OperationResult<ClaimDto>.ValidationFailed("לא ניתן לקחת לטיפול תביעה שנסגרה או בוטלה");
        }

        var appraiser = await _employees.GetByIdAsync(user.Id, cancellationToken);
        if (appraiser is null)
        {
            return OperationResult<ClaimDto>.Unauthorized("העובד המחובר לא נמצא");
        }

        if (appraiser.Role != EmployeeRole.Appraiser || appraiser.Specializations.All(s => s.Id != claim.InsuranceTypeId))
        {
            return OperationResult<ClaimDto>.Forbidden("התביעה אינה בתחום ההתמחות שלך");
        }

        if (claim.Version != dto.Version)
        {
            // The client read an older version - someone changed the claim in between.
            _logger.LogWarning("Appraiser {AppraiserId} tried to take claim {ClaimId} using a stale version", user.Id, claimId);
            return OperationResult<ClaimDto>.Conflict(ErrorMessages.ClaimTakenByAnother);
        }

        var now = _timeProvider.GetUtcNow().UtcDateTime;
        claim.HandlerId = appraiser.Id;
        claim.Status = ClaimStatus.InTreatment;
        claim.AssignedAt = now;
        _referrals.Add(new Referral { Claim = claim, AppraiserId = appraiser.Id, ReferredAt = now });

        try
        {
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (ConcurrencyConflictException)
        {
            // Lost the race: another appraiser saved between our read and our save (nothing was written).
            _logger.LogWarning("Concurrency conflict: appraiser {AppraiserId} lost the race for claim {ClaimId}", user.Id, claimId);
            return OperationResult<ClaimDto>.Conflict(ErrorMessages.ClaimTakenByAnother);
        }

        _logger.LogInformation("Claim {ClaimId} taken for handling by appraiser {AppraiserId}", claimId, user.Id);

        var result = _mapper.Map<ClaimDto>(claim);
        result.HandlerName = appraiser.FullName;
        return OperationResult<ClaimDto>.Success(result);
    }

    public async Task<OperationResult<ClaimDto>> UpdateStatusAsync(CurrentUser user, int claimId, ClaimStatusUpdateDto dto, CancellationToken cancellationToken = default)
    {
        var claim = await _claims.GetForUpdateAsync(claimId, cancellationToken);
        if (claim is null)
        {
            return OperationResult<ClaimDto>.NotFound(ClaimNotFound);
        }

        var newStatus = dto.Status!.Value;

        if (user.IsClient)
        {
            if (claim.ClientId != user.Id)
            {
                return OperationResult<ClaimDto>.Forbidden("ניתן לעדכן רק תביעה של הלקוח עצמו");
            }

            var allowed = newStatus switch
            {
                ClaimStatus.Opened => claim.Status == ClaimStatus.Draft,
                ClaimStatus.Cancelled => claim.Status is ClaimStatus.Draft or ClaimStatus.Opened or ClaimStatus.InTreatment,
                _ => false
            };

            if (!allowed)
            {
                return OperationResult<ClaimDto>.ValidationFailed(newStatus == ClaimStatus.Cancelled
                    ? "לא ניתן לבטל תביעה שאושרה, נדחתה או בוטלה"
                    : "הלקוח יכול רק להגיש טיוטה או לבטל תביעה");
            }
        }
        else if (user.IsAppraiser)
        {
            if (claim.HandlerId != user.Id)
            {
                return OperationResult<ClaimDto>.Forbidden("רק השמאי המטפל בתביעה רשאי לעדכן את הסטטוס שלה");
            }

            if (claim.Status != ClaimStatus.InTreatment)
            {
                return OperationResult<ClaimDto>.ValidationFailed("ניתן לעדכן סטטוס רק לתביעה שנמצאת בטיפול");
            }

            if (newStatus is not (ClaimStatus.Approved or ClaimStatus.Rejected))
            {
                return OperationResult<ClaimDto>.ValidationFailed("שמאי יכול רק לאשר או לדחות תביעה");
            }
        }
        else
        {
            return OperationResult<ClaimDto>.Forbidden("אין הרשאה לעדכן סטטוס של תביעה");
        }

        if (claim.Version != dto.Version)
        {
            return OperationResult<ClaimDto>.Conflict(ClaimModifiedByAnother);
        }

        claim.Status = newStatus;

        var conflict = await SaveOrConflictAsync(claimId, ClaimModifiedByAnother, cancellationToken);
        if (conflict is not null)
        {
            return conflict;
        }

        _logger.LogInformation("Claim {ClaimId} status changed to {Status} by {Role} {UserId}", claimId, newStatus, user.Role, user.Id);
        return OperationResult<ClaimDto>.Success(_mapper.Map<ClaimDto>(claim));
    }

    public async Task<OperationResult> DeleteAsync(CurrentUser user, int id, CancellationToken cancellationToken = default)
    {
        var claim = await _claims.GetForUpdateAsync(id, cancellationToken);
        if (claim is null)
        {
            return OperationResult.NotFound(ClaimNotFound);
        }

        if (!user.IsClient || claim.ClientId != user.Id)
        {
            return OperationResult.Forbidden("רק הלקוח שפתח את הטיוטה רשאי למחוק אותה");
        }

        if (claim.Status != ClaimStatus.Draft)
        {
            return OperationResult.ValidationFailed("ניתן למחוק רק טיוטה");
        }

        _claims.Remove(claim);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Draft claim {ClaimId} deleted", id);
        return OperationResult.Success();
    }

    /// <summary>Saves the unit of work; a lost optimistic-concurrency check becomes a Conflict result (null means saved).</summary>
    private async Task<OperationResult<ClaimDto>?> SaveOrConflictAsync(int claimId, string message, CancellationToken cancellationToken)
    {
        try
        {
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            return null;
        }
        catch (ConcurrencyConflictException)
        {
            _logger.LogWarning("Concurrency conflict while updating claim {ClaimId}", claimId);
            return OperationResult<ClaimDto>.Conflict(message);
        }
    }

    private async Task<bool> CanViewAsync(CurrentUser user, Claim claim, CancellationToken cancellationToken)
    {
        if (user.IsClient)
        {
            return claim.ClientId == user.Id;
        }

        if (claim.Status == ClaimStatus.Draft)
        {
            return false; // drafts are private to the client
        }

        if (user.IsManager)
        {
            return true;
        }

        if (!user.IsAppraiser)
        {
            return false;
        }

        if (claim.HandlerId == user.Id)
        {
            return true;
        }

        if (claim.Status != ClaimStatus.Opened || claim.HandlerId is not null)
        {
            return false;
        }

        var appraiser = await _employees.GetByIdAsync(user.Id, cancellationToken);
        return appraiser is not null && appraiser.Specializations.Any(s => s.Id == claim.InsuranceTypeId);
    }
}
