using AutoMapper;
using InsuranceClaims.Core.Common;
using InsuranceClaims.Core.DTOs;
using InsuranceClaims.Core.Entities;
using InsuranceClaims.Core.Interfaces.Repositories;
using InsuranceClaims.Core.Interfaces.Services;

namespace InsuranceClaims.Service.Services;

public class InsuranceTypeService : IInsuranceTypeService
{
    private const string NotFoundMessage = "סוג הביטוח לא נמצא";
    private const string NameExistsMessage = "סוג ביטוח בשם זה כבר קיים";

    private readonly IInsuranceTypeRepository _insuranceTypes;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public InsuranceTypeService(IInsuranceTypeRepository insuranceTypes, IUnitOfWork unitOfWork, IMapper mapper)
    {
        _insuranceTypes = insuranceTypes;
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<IReadOnlyList<InsuranceTypeDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var types = await _insuranceTypes.GetAllAsync(cancellationToken);
        return _mapper.Map<List<InsuranceTypeDto>>(types);
    }

    public async Task<OperationResult<InsuranceTypeDto>> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var type = await _insuranceTypes.GetByIdAsync(id, cancellationToken);
        return type is null
            ? OperationResult<InsuranceTypeDto>.NotFound(NotFoundMessage)
            : OperationResult<InsuranceTypeDto>.Success(_mapper.Map<InsuranceTypeDto>(type));
    }

    public async Task<OperationResult<InsuranceTypeDto>> CreateAsync(InsuranceTypeCreateDto dto, CancellationToken cancellationToken = default)
    {
        if (await _insuranceTypes.NameExistsAsync(dto.Name, cancellationToken: cancellationToken))
        {
            return OperationResult<InsuranceTypeDto>.Conflict(NameExistsMessage);
        }

        var type = _mapper.Map<InsuranceType>(dto);
        _insuranceTypes.Add(type);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return OperationResult<InsuranceTypeDto>.Success(_mapper.Map<InsuranceTypeDto>(type));
    }

    public async Task<OperationResult> UpdateAsync(int id, InsuranceTypeUpdateDto dto, CancellationToken cancellationToken = default)
    {
        var type = await _insuranceTypes.GetForUpdateAsync(id, cancellationToken);
        if (type is null)
        {
            return OperationResult.NotFound(NotFoundMessage);
        }

        if (await _insuranceTypes.NameExistsAsync(dto.Name, id, cancellationToken))
        {
            return OperationResult.Conflict(NameExistsMessage);
        }

        _mapper.Map(dto, type);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return OperationResult.Success();
    }

    public async Task<OperationResult> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var type = await _insuranceTypes.GetForUpdateAsync(id, cancellationToken);
        if (type is null)
        {
            return OperationResult.NotFound(NotFoundMessage);
        }

        if (await _insuranceTypes.HasPoliciesAsync(id, cancellationToken))
        {
            return OperationResult.Conflict("לא ניתן למחוק סוג ביטוח שיש לו פוליסות");
        }

        _insuranceTypes.Remove(type);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return OperationResult.Success();
    }
}
