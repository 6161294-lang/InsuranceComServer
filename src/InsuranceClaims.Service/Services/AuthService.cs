using AutoMapper;
using InsuranceClaims.Core.Common;
using InsuranceClaims.Core.DTOs;
using InsuranceClaims.Core.Entities;
using InsuranceClaims.Core.Interfaces.Repositories;
using InsuranceClaims.Core.Interfaces.Services;
using Microsoft.Extensions.Logging;

namespace InsuranceClaims.Service.Services;

public class AuthService : IAuthService
{
    private const string InvalidCredentials = "אימייל או סיסמה שגויים";

    private readonly IEmployeeRepository _employees;
    private readonly IClientRepository _clients;
    private readonly IInsuranceTypeRepository _insuranceTypes;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ITokenGenerator _tokenGenerator;
    private readonly IMapper _mapper;
    private readonly ILogger<AuthService> _logger;

    public AuthService(
        IEmployeeRepository employees,
        IClientRepository clients,
        IInsuranceTypeRepository insuranceTypes,
        IUnitOfWork unitOfWork,
        IPasswordHasher passwordHasher,
        ITokenGenerator tokenGenerator,
        IMapper mapper,
        ILogger<AuthService> logger)
    {
        _employees = employees;
        _clients = clients;
        _insuranceTypes = insuranceTypes;
        _unitOfWork = unitOfWork;
        _passwordHasher = passwordHasher;
        _tokenGenerator = tokenGenerator;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<OperationResult<ClientDto>> RegisterClientAsync(RegisterClientDto dto, CancellationToken cancellationToken = default)
    {
        var email = EmailNormalizer.Normalize(dto.Email);
        if (await _clients.EmailExistsAsync(email, cancellationToken) || await _employees.EmailExistsAsync(email, cancellationToken))
        {
            return OperationResult<ClientDto>.Conflict("כתובת האימייל כבר רשומה במערכת");
        }

        if (await _clients.NationalIdExistsAsync(dto.NationalId, cancellationToken))
        {
            return OperationResult<ClientDto>.Conflict("מספר הזיהוי כבר רשום במערכת");
        }

        var insuranceTypeIds = dto.InsuranceTypeIds.Distinct().ToList();
        var insuranceTypes = await _insuranceTypes.GetByIdsAsync(insuranceTypeIds, cancellationToken);
        if (insuranceTypes.Count != insuranceTypeIds.Count)
        {
            return OperationResult<ClientDto>.ValidationFailed("אחד או יותר מסוגי הביטוח לא נמצאו");
        }

        var client = _mapper.Map<Client>(dto);
        client.Email = email;
        client.PasswordHash = _passwordHasher.Hash(dto.Password);
        foreach (var insuranceType in insuranceTypes)
        {
            client.InsuranceTypes.Add(insuranceType);
        }

        _clients.Add(client);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // Only the new id is logged - never the registration details.
        _logger.LogInformation("Client {ClientId} registered", client.Id);
        return OperationResult<ClientDto>.Success(_mapper.Map<ClientDto>(client));
    }

    public async Task<OperationResult<AuthResponseDto>> LoginAsync(LoginDto dto, CancellationToken cancellationToken = default)
    {
        var email = EmailNormalizer.Normalize(dto.Email);

        var employee = await _employees.GetByEmailAsync(email, cancellationToken);
        if (employee is not null)
        {
            return CompleteLogin(dto.Password, employee.PasswordHash, employee.Id, employee.FullName, employee.Email,
                employee.Role.ToString(), _mapper.Map<UserDto>(employee));
        }

        var client = await _clients.GetByEmailAsync(email, cancellationToken);
        if (client is not null)
        {
            return CompleteLogin(dto.Password, client.PasswordHash, client.Id, client.FullName, client.Email,
                AppRoles.Client, _mapper.Map<UserDto>(client));
        }

        _logger.LogInformation("Failed login attempt");
        return OperationResult<AuthResponseDto>.Unauthorized(InvalidCredentials);
    }

    private OperationResult<AuthResponseDto> CompleteLogin(string password, string passwordHash, int id, string fullName,
        string email, string role, UserDto user)
    {
        if (!_passwordHasher.Verify(password, passwordHash))
        {
            _logger.LogInformation("Failed login attempt");
            return OperationResult<AuthResponseDto>.Unauthorized(InvalidCredentials);
        }

        var (token, expiresAt) = _tokenGenerator.GenerateToken(id, fullName, email, role);
        _logger.LogInformation("{Role} {UserId} logged in", role, id);

        return OperationResult<AuthResponseDto>.Success(new AuthResponseDto
        {
            Token = token,
            ExpiresAt = expiresAt,
            User = user
        });
    }
}
