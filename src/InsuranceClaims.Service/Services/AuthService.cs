using AutoMapper;
using InsuranceClaims.Core.Common;
using InsuranceClaims.Core.DTOs;
using InsuranceClaims.Core.Entities;
using InsuranceClaims.Core.Enums;
using InsuranceClaims.Core.Interfaces.Repositories;
using InsuranceClaims.Core.Interfaces.Services;
using Microsoft.Extensions.Logging;

namespace InsuranceClaims.Service.Services;

public class AuthService : IAuthService
{
    private const string InvalidCredentials = "שם משתמש או סיסמה שגויים";

    private readonly IEmployeeRepository _employees;
    private readonly IInsuranceTypeRepository _insuranceTypes;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ITokenGenerator _tokenGenerator;
    private readonly IMapper _mapper;
    private readonly ILogger<AuthService> _logger;

    public AuthService(
        IEmployeeRepository employees,
        IInsuranceTypeRepository insuranceTypes,
        IUnitOfWork unitOfWork,
        IPasswordHasher passwordHasher,
        ITokenGenerator tokenGenerator,
        IMapper mapper,
        ILogger<AuthService> logger)
    {
        _employees = employees;
        _insuranceTypes = insuranceTypes;
        _unitOfWork = unitOfWork;
        _passwordHasher = passwordHasher;
        _tokenGenerator = tokenGenerator;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<OperationResult<EmployeeDto>> RegisterAsync(RegisterDto dto, CancellationToken cancellationToken = default)
    {
        if (await _employees.UsernameExistsAsync(dto.Username, cancellationToken))
        {
            return OperationResult<EmployeeDto>.Conflict("שם המשתמש כבר תפוס");
        }

        var specializations = await _insuranceTypes.GetByIdsAsync(dto.SpecializationIds, cancellationToken);
        if (specializations.Count != dto.SpecializationIds.Distinct().Count())
        {
            return OperationResult<EmployeeDto>.ValidationFailed("אחד או יותר מתחומי ההתמחות לא נמצאו");
        }

        var employee = _mapper.Map<Employee>(dto);
        employee.Role = EmployeeRole.Adjuster; // self-registration never grants the manager role
        employee.PasswordHash = _passwordHasher.Hash(dto.Password);
        foreach (var specialization in specializations)
        {
            employee.Specializations.Add(specialization);
        }

        _employees.Add(employee);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Employee {EmployeeId} registered", employee.Id);
        return OperationResult<EmployeeDto>.Success(_mapper.Map<EmployeeDto>(employee));
    }

    public async Task<OperationResult<AuthResponseDto>> LoginAsync(LoginDto dto, CancellationToken cancellationToken = default)
    {
        var employee = await _employees.GetByUsernameAsync(dto.Username, cancellationToken);
        if (employee is null || !_passwordHasher.Verify(dto.Password, employee.PasswordHash))
        {
            _logger.LogInformation("Failed login attempt");
            return OperationResult<AuthResponseDto>.Unauthorized(InvalidCredentials);
        }

        var (token, expiresAt) = _tokenGenerator.GenerateToken(employee);
        _logger.LogInformation("Employee {EmployeeId} logged in", employee.Id);

        return OperationResult<AuthResponseDto>.Success(new AuthResponseDto
        {
            Token = token,
            ExpiresAt = expiresAt,
            Employee = _mapper.Map<EmployeeDto>(employee)
        });
    }
}
