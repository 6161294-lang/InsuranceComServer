using AutoMapper;
using InsuranceClaims.Core.Common;
using InsuranceClaims.Core.DTOs;
using InsuranceClaims.Core.Interfaces.Repositories;
using InsuranceClaims.Core.Interfaces.Services;
using Microsoft.Extensions.Logging;

namespace InsuranceClaims.Service.Services;

public class ReferralService : IReferralService
{
    private const string ReferralNotFound = "ההפניה לא נמצאה";
    private const string AppraiserOnly = "רק שמאי רשאי לצפות בהפניות";
    private const string NotYourReferral = "ההפניה אינה משויכת אליך";

    private readonly IReferralRepository _referrals;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly ILogger<ReferralService> _logger;

    public ReferralService(
        IReferralRepository referrals,
        IUnitOfWork unitOfWork,
        IMapper mapper,
        ILogger<ReferralService> logger)
    {
        _referrals = referrals;
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<OperationResult<PagedResult<ReferralDto>>> GetPagedAsync(CurrentUser user, PagingParameters paging, CancellationToken cancellationToken = default)
    {
        if (!user.IsAppraiser)
        {
            return OperationResult<PagedResult<ReferralDto>>.Forbidden(AppraiserOnly);
        }

        var (items, totalCount) = await _referrals.GetPagedByAppraiserAsync(user.Id, paging, cancellationToken);
        return OperationResult<PagedResult<ReferralDto>>.Success(new PagedResult<ReferralDto>
        {
            Items = _mapper.Map<List<ReferralDto>>(items),
            TotalCount = totalCount,
            Page = paging.Page,
            PageSize = paging.PageSize
        });
    }

    public async Task<OperationResult<ReferralDto>> GetByIdAsync(CurrentUser user, int id, CancellationToken cancellationToken = default)
    {
        if (!user.IsAppraiser)
        {
            return OperationResult<ReferralDto>.Forbidden(AppraiserOnly);
        }

        var referral = await _referrals.GetByIdAsync(id, cancellationToken);
        if (referral is null)
        {
            return OperationResult<ReferralDto>.NotFound(ReferralNotFound);
        }

        return referral.AppraiserId == user.Id
            ? OperationResult<ReferralDto>.Success(_mapper.Map<ReferralDto>(referral))
            : OperationResult<ReferralDto>.Forbidden(NotYourReferral);
    }

    public async Task<OperationResult<ReferralDto>> UpdateAsync(CurrentUser user, int id, ReferralUpdateDto dto, CancellationToken cancellationToken = default)
    {
        if (!user.IsAppraiser)
        {
            return OperationResult<ReferralDto>.Forbidden(AppraiserOnly);
        }

        var referral = await _referrals.GetForUpdateAsync(id, cancellationToken);
        if (referral is null)
        {
            return OperationResult<ReferralDto>.NotFound(ReferralNotFound);
        }

        if (referral.AppraiserId != user.Id)
        {
            return OperationResult<ReferralDto>.Forbidden(NotYourReferral);
        }

        _mapper.Map(dto, referral);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Referral {ReferralId} updated by appraiser {AppraiserId}", id, user.Id);
        return OperationResult<ReferralDto>.Success(_mapper.Map<ReferralDto>(referral));
    }
}
