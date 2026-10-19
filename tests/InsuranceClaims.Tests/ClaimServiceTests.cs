using InsuranceClaims.Core.Common;
using InsuranceClaims.Core.DTOs;
using InsuranceClaims.Core.Entities;
using InsuranceClaims.Core.Enums;
using InsuranceClaims.Core.Interfaces.Repositories;
using InsuranceClaims.Service.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace InsuranceClaims.Tests;

public class ClaimServiceTests
{
    private const int CarInsuranceTypeId = 1;
    private const int HomeInsuranceTypeId = 2;
    private const int AdjusterId = 2;
    private const int OtherAdjusterId = 3;

    private static readonly DateTimeOffset Now = new(2026, 10, 1, 12, 0, 0, TimeSpan.Zero);
    private static readonly Guid CurrentVersion = Guid.NewGuid();

    private readonly Mock<IClaimRepository> _claims = new();
    private readonly Mock<IPolicyRepository> _policies = new();
    private readonly Mock<IEmployeeRepository> _employees = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly ClaimService _service;

    public ClaimServiceTests()
    {
        _service = new ClaimService(
            _claims.Object,
            _policies.Object,
            _employees.Object,
            _unitOfWork.Object,
            TestMapper.Create(),
            new FixedTimeProvider(Now),
            NullLogger<ClaimService>.Instance);
    }

    [Fact]
    public async Task TakeAsync_OpenClaimInSpecialization_AssignsHandlerAndSaves()
    {
        var claim = OpenCarClaim();
        SetupClaim(claim);
        SetupEmployee(Adjuster(AdjusterId, CarInsuranceTypeId));

        var result = await _service.TakeAsync(claim.Id, AdjusterId, new ClaimHandlerUpdateDto { Version = CurrentVersion });

        Assert.Equal(ResultStatus.Success, result.Status);
        Assert.Equal(AdjusterId, claim.HandlerId);
        Assert.Equal(ClaimStatus.InProgress, claim.Status);
        Assert.Equal(Now.UtcDateTime, claim.AssignedAt);
        Assert.Equal(AdjusterId, result.Value!.HandlerId);
        Assert.Equal("שמאי 2", result.Value.HandlerName);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task TakeAsync_ClaimAlreadyInProgress_ReturnsConflictWithoutSaving()
    {
        var claim = OpenCarClaim();
        claim.Status = ClaimStatus.InProgress;
        claim.HandlerId = OtherAdjusterId;
        SetupClaim(claim);
        SetupEmployee(Adjuster(AdjusterId, CarInsuranceTypeId));

        var result = await _service.TakeAsync(claim.Id, AdjusterId, new ClaimHandlerUpdateDto { Version = CurrentVersion });

        Assert.Equal(ResultStatus.Conflict, result.Status);
        Assert.Equal(ErrorMessages.ClaimTakenByAnother, result.Error);
        Assert.Equal(OtherAdjusterId, claim.HandlerId);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task TakeAsync_AdjusterOutsideSpecialization_ReturnsForbiddenWithoutSaving()
    {
        var claim = OpenCarClaim();
        SetupClaim(claim);
        SetupEmployee(Adjuster(AdjusterId, HomeInsuranceTypeId));

        var result = await _service.TakeAsync(claim.Id, AdjusterId, new ClaimHandlerUpdateDto { Version = CurrentVersion });

        Assert.Equal(ResultStatus.Forbidden, result.Status);
        Assert.Null(claim.HandlerId);
        Assert.Equal(ClaimStatus.Open, claim.Status);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task TakeAsync_LosesRaceAtSave_ReturnsConflict()
    {
        var claim = OpenCarClaim();
        SetupClaim(claim);
        SetupEmployee(Adjuster(AdjusterId, CarInsuranceTypeId));
        _unitOfWork
            .Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ConcurrencyConflictException("conflict", new Exception()));

        var result = await _service.TakeAsync(claim.Id, AdjusterId, new ClaimHandlerUpdateDto { Version = CurrentVersion });

        Assert.Equal(ResultStatus.Conflict, result.Status);
        Assert.Equal(ErrorMessages.ClaimTakenByAnother, result.Error);
    }

    [Fact]
    public async Task TakeAsync_StaleVersion_ReturnsConflictWithoutSaving()
    {
        var claim = OpenCarClaim();
        SetupClaim(claim);
        SetupEmployee(Adjuster(AdjusterId, CarInsuranceTypeId));

        var result = await _service.TakeAsync(claim.Id, AdjusterId, new ClaimHandlerUpdateDto { Version = Guid.NewGuid() });

        Assert.Equal(ResultStatus.Conflict, result.Status);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task TakeAsync_MissingClaim_ReturnsNotFound()
    {
        _claims.Setup(r => r.GetForUpdateAsync(It.IsAny<int>(), It.IsAny<CancellationToken>())).ReturnsAsync((Claim?)null);

        var result = await _service.TakeAsync(99, AdjusterId, new ClaimHandlerUpdateDto { Version = CurrentVersion });

        Assert.Equal(ResultStatus.NotFound, result.Status);
    }

    [Fact]
    public async Task UpdateStatusAsync_ByEmployeeWhoIsNotTheHandler_ReturnsForbidden()
    {
        var claim = OpenCarClaim();
        claim.Status = ClaimStatus.InProgress;
        claim.HandlerId = OtherAdjusterId;
        SetupClaim(claim);

        var result = await _service.UpdateStatusAsync(claim.Id, AdjusterId,
            new ClaimStatusUpdateDto { Status = ClaimStatus.Approved, Version = CurrentVersion });

        Assert.Equal(ResultStatus.Forbidden, result.Status);
        Assert.Equal(ClaimStatus.InProgress, claim.Status);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task UpdateStatusAsync_ByHandler_ChangesStatus()
    {
        var claim = OpenCarClaim();
        claim.Status = ClaimStatus.InProgress;
        claim.HandlerId = AdjusterId;
        SetupClaim(claim);

        var result = await _service.UpdateStatusAsync(claim.Id, AdjusterId,
            new ClaimStatusUpdateDto { Status = ClaimStatus.Approved, Version = CurrentVersion });

        Assert.Equal(ResultStatus.Success, result.Status);
        Assert.Equal(ClaimStatus.Approved, claim.Status);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_OnExpiredPolicy_ReturnsValidationFailed()
    {
        var expired = CarPolicy();
        expired.StartDate = new DateTime(2022, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        expired.EndDate = new DateTime(2023, 12, 31, 0, 0, 0, DateTimeKind.Utc);
        _policies.Setup(r => r.GetByIdAsync(expired.Id, It.IsAny<CancellationToken>())).ReturnsAsync(expired);

        var result = await _service.CreateAsync(new ClaimCreateDto
        {
            PolicyId = expired.Id,
            Title = "תאונה",
            Description = "פגיעה בדלת",
            Amount = 1000m
        });

        Assert.Equal(ResultStatus.ValidationFailed, result.Status);
        _claims.Verify(r => r.Add(It.IsAny<Claim>()), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_OnActivePolicy_CreatesOpenClaim()
    {
        var policy = CarPolicy();
        _policies.Setup(r => r.GetByIdAsync(policy.Id, It.IsAny<CancellationToken>())).ReturnsAsync(policy);

        var result = await _service.CreateAsync(new ClaimCreateDto
        {
            PolicyId = policy.Id,
            Title = "תאונה",
            Description = "פגיעה בדלת",
            Amount = 1000m
        });

        Assert.Equal(ResultStatus.Success, result.Status);
        Assert.Equal(ClaimStatus.Open, result.Value!.Status);
        Assert.Null(result.Value.HandlerId);
        _claims.Verify(r => r.Add(It.Is<Claim>(c => c.PolicyId == policy.Id && c.Status == ClaimStatus.Open)), Times.Once);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    private void SetupClaim(Claim claim) =>
        _claims.Setup(r => r.GetForUpdateAsync(claim.Id, It.IsAny<CancellationToken>())).ReturnsAsync(claim);

    private void SetupEmployee(Employee employee) =>
        _employees.Setup(r => r.GetByIdAsync(employee.Id, It.IsAny<CancellationToken>())).ReturnsAsync(employee);

    private static Policy CarPolicy() => new()
    {
        Id = 1,
        PolicyNumber = "CAR-1001",
        HolderName = "ישראל ישראלי",
        InsuranceTypeId = CarInsuranceTypeId,
        InsuranceType = new InsuranceType { Id = CarInsuranceTypeId, Name = "ביטוח רכב" },
        StartDate = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc),
        EndDate = new DateTime(2030, 12, 31, 0, 0, 0, DateTimeKind.Utc)
    };

    private static Claim OpenCarClaim()
    {
        var policy = CarPolicy();
        return new Claim
        {
            Id = 10,
            Title = "תאונה בצומת",
            Description = "פגיעה בדלת",
            Amount = 5000m,
            Status = ClaimStatus.Open,
            PolicyId = policy.Id,
            Policy = policy,
            CreatedAt = Now.UtcDateTime.AddDays(-3),
            Version = CurrentVersion
        };
    }

    private static Employee Adjuster(int id, params int[] specializationIds) => new()
    {
        Id = id,
        FullName = $"שמאי {id}",
        Username = $"adjuster{id}",
        Role = EmployeeRole.Adjuster,
        Specializations = specializationIds.Select(t => new InsuranceType { Id = t, Name = $"type {t}" }).ToList()
    };
}
