using AutoMapper;
using InsuranceClaims.Core.Common;
using InsuranceClaims.Core.DTOs;
using InsuranceClaims.Core.Entities;
using InsuranceClaims.Core.Interfaces.Repositories;
using InsuranceClaims.Core.Interfaces.Services;
using Microsoft.Extensions.Logging;

namespace InsuranceClaims.Service.Services;

public class InsuranceTypeService : IInsuranceTypeService
{
    private const string NotFound = "סוג הביטוח לא נמצא";
    private const string ManagerOnly = "רק מנהל רשאי לנהל סוגי ביטוח";

    private readonly IInsuranceTypeRepository _insuranceTypes;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly ILogger<InsuranceTypeService> _logger;

    public InsuranceTypeService(
        IInsuranceTypeRepository insuranceTypes,
        IUnitOfWork unitOfWork,
        IMapper mapper,
        ILogger<InsuranceTypeService> logger)
    {
        _insuranceTypes = insuranceTypes;
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<IReadOnlyList<InsuranceTypeDto>> GetAllAsync(CancellationToken cancellationToken = default) =>
        _mapper.Map<List<InsuranceTypeDto>>(await _insuranceTypes.GetAllAsync(cancellationToken));

    public async Task<OperationResult<InsuranceTypeDto>> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var insuranceType = await _insuranceTypes.GetByIdAsync(id, cancellationToken);
        return insuranceType is null
            ? OperationResult<InsuranceTypeDto>.NotFound(NotFound)
            : OperationResult<InsuranceTypeDto>.Success(_mapper.Map<InsuranceTypeDto>(insuranceType));
    }

    public async Task<OperationResult<InsuranceTypeDto>> CreateAsync(CurrentUser user, InsuranceTypeCreateDto dto, CancellationToken cancellationToken = default)
    {
        if (!user.IsManager)
        {
            return OperationResult<InsuranceTypeDto>.Forbidden(ManagerOnly);
        }

        if (await _insuranceTypes.NameExistsAsync(dto.Name, cancellationToken: cancellationToken))
        {
            return OperationResult<InsuranceTypeDto>.Conflict("סוג ביטוח בשם זה כבר קיים");
        }

        var insuranceType = _mapper.Map<InsuranceType>(dto);
        _insuranceTypes.Add(insuranceType);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Insurance type {InsuranceTypeId} created", insuranceType.Id);
        return OperationResult<InsuranceTypeDto>.Success(_mapper.Map<InsuranceTypeDto>(insuranceType));
    }

    public async Task<OperationResult> UpdateAsync(CurrentUser user, int id, InsuranceTypeUpdateDto dto, CancellationToken cancellationToken = default)
    {
        if (!user.IsManager)
        {
            return OperationResult.Forbidden(ManagerOnly);
        }

        var insuranceType = await _insuranceTypes.GetForUpdateAsync(id, cancellationToken);
        if (insuranceType is null)
        {
            return OperationResult.NotFound(NotFound);
        }

        if (await _insuranceTypes.NameExistsAsync(dto.Name, id, cancellationToken))
        {
            return OperationResult.Conflict("סוג ביטוח בשם זה כבר קיים");
        }

        _mapper.Map(dto, insuranceType);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return OperationResult.Success();
    }

    public async Task<OperationResult> DeleteAsync(CurrentUser user, int id, CancellationToken cancellationToken = default)
    {
        if (!user.IsManager)
        {
            return OperationResult.Forbidden(ManagerOnly);
        }

        var insuranceType = await _insuranceTypes.GetForUpdateAsync(id, cancellationToken);
        if (insuranceType is null)
        {
            return OperationResult.NotFound(NotFound);
        }

        if (await _insuranceTypes.IsInUseAsync(id, cancellationToken))
        {
            return OperationResult.Conflict("לא ניתן למחוק סוג ביטוח שמשויכים אליו תביעות, לקוחות או שמאים");
        }

        _insuranceTypes.Remove(insuranceType);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Insurance type {InsuranceTypeId} deleted", id);
        return OperationResult.Success();
    }
}
