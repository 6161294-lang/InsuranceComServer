using AutoMapper;
using InsuranceClaims.Core.Common;
using InsuranceClaims.Core.DTOs;
using InsuranceClaims.Core.Entities;
using InsuranceClaims.Core.Enums;
using InsuranceClaims.Core.Interfaces.Repositories;
using InsuranceClaims.Core.Interfaces.Services;
using Microsoft.Extensions.Logging;

namespace InsuranceClaims.Service.Services;

public class EmployeeService : IEmployeeService
{
    private const string ManagerOnly = "רק מנהל רשאי לנהל עובדים";
    private const string EmployeeNotFound = "העובד לא נמצא";

    private readonly IEmployeeRepository _employees;
    private readonly IClientRepository _clients;
    private readonly IInsuranceTypeRepository _insuranceTypes;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IMapper _mapper;
    private readonly ILogger<EmployeeService> _logger;

    public EmployeeService(
        IEmployeeRepository employees,
        IClientRepository clients,
        IInsuranceTypeRepository insuranceTypes,
        IUnitOfWork unitOfWork,
        IPasswordHasher passwordHasher,
        IMapper mapper,
        ILogger<EmployeeService> logger)
    {
        _employees = employees;
        _clients = clients;
        _insuranceTypes = insuranceTypes;
        _unitOfWork = unitOfWork;
        _passwordHasher = passwordHasher;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<OperationResult<PagedResult<EmployeeDto>>> GetPagedAsync(CurrentUser user, PagingParameters paging, CancellationToken cancellationToken = default)
    {
        if (!user.IsManager)
        {
            return OperationResult<PagedResult<EmployeeDto>>.Forbidden(ManagerOnly);
        }

        var (items, totalCount) = await _employees.GetPagedAsync(paging, cancellationToken);
        return OperationResult<PagedResult<EmployeeDto>>.Success(new PagedResult<EmployeeDto>
        {
            Items = _mapper.Map<List<EmployeeDto>>(items),
            TotalCount = totalCount,
            Page = paging.Page,
            PageSize = paging.PageSize
        });
    }

    public async Task<OperationResult<EmployeeDto>> GetByIdAsync(CurrentUser user, int id, CancellationToken cancellationToken = default)
    {
        if (!user.IsManager)
        {
            return OperationResult<EmployeeDto>.Forbidden(ManagerOnly);
        }

        var employee = await _employees.GetByIdAsync(id, cancellationToken);
        return employee is null
            ? OperationResult<EmployeeDto>.NotFound(EmployeeNotFound)
            : OperationResult<EmployeeDto>.Success(_mapper.Map<EmployeeDto>(employee));
    }

    public async Task<OperationResult<EmployeeDto>> CreateAsync(CurrentUser user, EmployeeCreateDto dto, CancellationToken cancellationToken = default)
    {
        if (!user.IsManager)
        {
            return OperationResult<EmployeeDto>.Forbidden(ManagerOnly);
        }

        var email = EmailNormalizer.Normalize(dto.Email);
        if (await _employees.EmailExistsAsync(email, cancellationToken) || await _clients.EmailExistsAsync(email, cancellationToken))
        {
            return OperationResult<EmployeeDto>.Conflict("כתובת האימייל כבר רשומה במערכת");
        }

        var role = dto.Role!.Value;
        var specializationIds = role == EmployeeRole.Appraiser ? dto.SpecializationIds.Distinct().ToList() : new List<int>();
        if (role == EmployeeRole.Appraiser && specializationIds.Count == 0)
        {
            return OperationResult<EmployeeDto>.ValidationFailed("יש לבחור לפחות תחום התמחות אחד לשמאי");
        }

        var specializations = await _insuranceTypes.GetByIdsAsync(specializationIds, cancellationToken);
        if (specializations.Count != specializationIds.Count)
        {
            return OperationResult<EmployeeDto>.ValidationFailed("אחד או יותר מתחומי ההתמחות לא נמצאו");
        }

        var employee = _mapper.Map<Employee>(dto);
        employee.Email = email;
        employee.Role = role;
        employee.PasswordHash = _passwordHasher.Hash(dto.Password);
        foreach (var specialization in specializations)
        {
            employee.Specializations.Add(specialization);
        }

        _employees.Add(employee);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Employee {EmployeeId} with role {Role} created by manager {ManagerId}", employee.Id, role, user.Id);
        return OperationResult<EmployeeDto>.Success(_mapper.Map<EmployeeDto>(employee));
    }

    public async Task<OperationResult> DeleteAsync(CurrentUser user, int id, CancellationToken cancellationToken = default)
    {
        if (!user.IsManager)
        {
            return OperationResult.Forbidden(ManagerOnly);
        }

        if (id == user.Id)
        {
            return OperationResult.ValidationFailed("לא ניתן למחוק את המשתמש המחובר");
        }

        var employee = await _employees.GetForUpdateAsync(id, cancellationToken);
        if (employee is null)
        {
            return OperationResult.NotFound(EmployeeNotFound);
        }

        if (await _employees.HasWorkHistoryAsync(id, cancellationToken))
        {
            return OperationResult.Conflict("לא ניתן למחוק עובד שטיפל בתביעות או קיבל הפניות");
        }

        _employees.Remove(employee);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Employee {EmployeeId} deleted by manager {ManagerId}", id, user.Id);
        return OperationResult.Success();
    }
}
