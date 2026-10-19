using AutoMapper;
using InsuranceClaims.Core.Common;
using InsuranceClaims.Core.DTOs;
using InsuranceClaims.Core.Interfaces.Repositories;
using InsuranceClaims.Core.Interfaces.Services;

namespace InsuranceClaims.Service.Services;

public class EmployeeService : IEmployeeService
{
    private const string NotFoundMessage = "העובד לא נמצא";

    private readonly IEmployeeRepository _employees;
    private readonly IInsuranceTypeRepository _insuranceTypes;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public EmployeeService(IEmployeeRepository employees, IInsuranceTypeRepository insuranceTypes, IUnitOfWork unitOfWork, IMapper mapper)
    {
        _employees = employees;
        _insuranceTypes = insuranceTypes;
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<PagedResult<EmployeeDto>> GetPagedAsync(PagingParameters paging, CancellationToken cancellationToken = default)
    {
        var (items, totalCount) = await _employees.GetPagedAsync(paging, cancellationToken);
        return new PagedResult<EmployeeDto>
        {
            Items = _mapper.Map<List<EmployeeDto>>(items),
            TotalCount = totalCount,
            Page = paging.Page,
            PageSize = paging.PageSize
        };
    }

    public async Task<OperationResult<EmployeeDto>> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var employee = await _employees.GetByIdAsync(id, cancellationToken);
        return employee is null
            ? OperationResult<EmployeeDto>.NotFound(NotFoundMessage)
            : OperationResult<EmployeeDto>.Success(_mapper.Map<EmployeeDto>(employee));
    }

    public async Task<OperationResult<EmployeeDto>> UpdateAsync(int id, EmployeeUpdateDto dto, CancellationToken cancellationToken = default)
    {
        var employee = await _employees.GetForUpdateAsync(id, cancellationToken);
        if (employee is null)
        {
            return OperationResult<EmployeeDto>.NotFound(NotFoundMessage);
        }

        var specializations = await _insuranceTypes.GetByIdsAsync(dto.SpecializationIds, cancellationToken);
        if (specializations.Count != dto.SpecializationIds.Distinct().Count())
        {
            return OperationResult<EmployeeDto>.ValidationFailed("אחד או יותר מתחומי ההתמחות לא נמצאו");
        }

        _mapper.Map(dto, employee);
        employee.Specializations.Clear();
        foreach (var specialization in specializations)
        {
            employee.Specializations.Add(specialization);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return OperationResult<EmployeeDto>.Success(_mapper.Map<EmployeeDto>(employee));
    }

    public async Task<OperationResult> DeleteAsync(int id, int currentEmployeeId, CancellationToken cancellationToken = default)
    {
        if (id == currentEmployeeId)
        {
            return OperationResult.ValidationFailed("לא ניתן למחוק את המשתמש המחובר");
        }

        var employee = await _employees.GetForUpdateAsync(id, cancellationToken);
        if (employee is null)
        {
            return OperationResult.NotFound(NotFoundMessage);
        }

        if (await _employees.HasClaimsInProgressAsync(id, cancellationToken))
        {
            return OperationResult.Conflict("לא ניתן למחוק עובד שמטפל כעת בתביעות");
        }

        _employees.Remove(employee);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return OperationResult.Success();
    }
}
