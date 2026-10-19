using AutoMapper;
using InsuranceClaims.Core.Common;
using InsuranceClaims.Core.DTOs;
using InsuranceClaims.Core.Entities;
using InsuranceClaims.Core.Interfaces.Repositories;
using InsuranceClaims.Core.Interfaces.Services;

namespace InsuranceClaims.Service.Services;

public class PolicyService : IPolicyService
{
    private const string NotFoundMessage = "הפוליסה לא נמצאה";
    private const string InvalidDatesMessage = "תאריך הסיום חייב להיות אחרי תאריך ההתחלה";

    private readonly IPolicyRepository _policies;
    private readonly IInsuranceTypeRepository _insuranceTypes;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public PolicyService(IPolicyRepository policies, IInsuranceTypeRepository insuranceTypes, IUnitOfWork unitOfWork, IMapper mapper)
    {
        _policies = policies;
        _insuranceTypes = insuranceTypes;
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<PagedResult<PolicyDto>> GetPagedAsync(PolicyQueryParameters query, CancellationToken cancellationToken = default)
    {
        var (items, totalCount) = await _policies.GetPagedAsync(query, cancellationToken);
        return new PagedResult<PolicyDto>
        {
            Items = _mapper.Map<List<PolicyDto>>(items),
            TotalCount = totalCount,
            Page = query.Page,
            PageSize = query.PageSize
        };
    }

    public async Task<OperationResult<PolicyDto>> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var policy = await _policies.GetByIdAsync(id, cancellationToken);
        return policy is null
            ? OperationResult<PolicyDto>.NotFound(NotFoundMessage)
            : OperationResult<PolicyDto>.Success(_mapper.Map<PolicyDto>(policy));
    }

    public async Task<OperationResult<PolicyDto>> CreateAsync(PolicyCreateDto dto, CancellationToken cancellationToken = default)
    {
        if (dto.EndDate <= dto.StartDate)
        {
            return OperationResult<PolicyDto>.ValidationFailed(InvalidDatesMessage);
        }

        var insuranceType = await _insuranceTypes.GetByIdAsync(dto.InsuranceTypeId, cancellationToken);
        if (insuranceType is null)
        {
            return OperationResult<PolicyDto>.ValidationFailed("סוג הביטוח לא נמצא");
        }

        if (await _policies.PolicyNumberExistsAsync(dto.PolicyNumber, cancellationToken))
        {
            return OperationResult<PolicyDto>.Conflict("פוליסה עם מספר זה כבר קיימת");
        }

        var policy = _mapper.Map<Policy>(dto);
        _policies.Add(policy);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var result = _mapper.Map<PolicyDto>(policy);
        result.InsuranceTypeName = insuranceType.Name;
        return OperationResult<PolicyDto>.Success(result);
    }

    public async Task<OperationResult> UpdateAsync(int id, PolicyUpdateDto dto, CancellationToken cancellationToken = default)
    {
        if (dto.EndDate <= dto.StartDate)
        {
            return OperationResult.ValidationFailed(InvalidDatesMessage);
        }

        var policy = await _policies.GetForUpdateAsync(id, cancellationToken);
        if (policy is null)
        {
            return OperationResult.NotFound(NotFoundMessage);
        }

        _mapper.Map(dto, policy);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return OperationResult.Success();
    }

    public async Task<OperationResult> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var policy = await _policies.GetForUpdateAsync(id, cancellationToken);
        if (policy is null)
        {
            return OperationResult.NotFound(NotFoundMessage);
        }

        if (await _policies.HasClaimsAsync(id, cancellationToken))
        {
            return OperationResult.Conflict("לא ניתן למחוק פוליסה שיש לה תביעות");
        }

        _policies.Remove(policy);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return OperationResult.Success();
    }
}
