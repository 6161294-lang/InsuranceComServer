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
    private static readonly DateTimeOffset Now = new(2026, 10, 1, 12, 0, 0, TimeSpan.Zero);
    private static readonly Guid CurrentVersion = Guid.NewGuid();

    private readonly Mock<IClaimRepository> _claims = new();
    private readonly Mock<IClientRepository> _clients = new();
    private readonly Mock<IInsuranceTypeRepository> _insuranceTypes = new();
    private readonly Mock<IEmployeeRepository> _employees = new();
    private readonly Mock<IReferralRepository> _referrals = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly ClaimService _service;

    public ClaimServiceTests()
    {
        _service = new ClaimService(
            _claims.Object,
            _clients.Object,
            _insuranceTypes.Object,
            _employees.Object,
            _referrals.Object,
            _unitOfWork.Object,
            TestMapper.Create(),
            new FixedTimeProvider(Now),
            NullLogger<ClaimService>.Instance);
    }

    // ---------- open a claim ----------

    [Fact]
    public async Task CreateAsync_ClientHoldingTheInsuranceType_OpensClaimOwnedByClient()
    {
        SetupClient(TestData.NewClient(TestData.ClientId, TestData.CarType()));

        var result = await _service.CreateAsync(TestData.ClientUser, NewClaimDto());

        Assert.Equal(ResultStatus.Success, result.Status);
        _claims.Verify(c => c.Add(It.Is<Claim>(claim =>
            claim.ClientId == TestData.ClientId &&
            claim.InsuranceTypeId == TestData.CarTypeId &&
            claim.Status == ClaimStatus.Opened &&
            claim.OpenedAt == Now.UtcDateTime &&
            claim.ClaimNumber.StartsWith("CLM-"))), Times.Once);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        Assert.Equal("ביטוח רכב", result.Value!.InsuranceTypeName);
    }

    [Fact]
    public async Task CreateAsync_SaveAsDraft_CreatesDraft()
    {
        SetupClient(TestData.NewClient(TestData.ClientId, TestData.CarType()));

        var dto = NewClaimDto();
        dto.SaveAsDraft = true;
        var result = await _service.CreateAsync(TestData.ClientUser, dto);

        Assert.Equal(ResultStatus.Success, result.Status);
        Assert.Equal(ClaimStatus.Draft, result.Value!.Status);
    }

    [Fact]
    public async Task CreateAsync_InsuranceTypeTheClientDoesNotHold_IsRejected()
    {
        SetupClient(TestData.NewClient(TestData.ClientId, TestData.HomeType()));

        var result = await _service.CreateAsync(TestData.ClientUser, NewClaimDto());

        Assert.Equal(ResultStatus.ValidationFailed, result.Status);
        _claims.Verify(c => c.Add(It.IsAny<Claim>()), Times.Never);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Theory]
    [InlineData(AppRoles.Manager)]
    [InlineData(AppRoles.Appraiser)]
    public async Task CreateAsync_EmployeeCannotOpenClaim(string role)
    {
        var result = await _service.CreateAsync(new CurrentUser(TestData.ManagerId, role), NewClaimDto());

        Assert.Equal(ResultStatus.Forbidden, result.Status);
        _claims.Verify(c => c.Add(It.IsAny<Claim>()), Times.Never);
    }

    // ---------- cancel / delete ----------

    [Theory]
    [InlineData(ClaimStatus.Draft)]
    [InlineData(ClaimStatus.Opened)]
    [InlineData(ClaimStatus.InTreatment)]
    public async Task UpdateStatusAsync_ClientCancelsOwnUnfinishedClaim_Cancels(ClaimStatus current)
    {
        var claim = TestData.NewClaim(CurrentVersion, current);
        SetupClaimForUpdate(claim);

        var result = await _service.UpdateStatusAsync(TestData.ClientUser, claim.Id, StatusDto(ClaimStatus.Cancelled));

        Assert.Equal(ResultStatus.Success, result.Status);
        Assert.Equal(ClaimStatus.Cancelled, claim.Status);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Theory]
    [InlineData(ClaimStatus.Approved)]
    [InlineData(ClaimStatus.Rejected)]
    [InlineData(ClaimStatus.Cancelled)]
    public async Task UpdateStatusAsync_ClientCannotCancelFinishedClaim(ClaimStatus current)
    {
        var claim = TestData.NewClaim(CurrentVersion, current);
        SetupClaimForUpdate(claim);

        var result = await _service.UpdateStatusAsync(TestData.ClientUser, claim.Id, StatusDto(ClaimStatus.Cancelled));

        Assert.Equal(ResultStatus.ValidationFailed, result.Status);
        Assert.Equal(current, claim.Status);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task UpdateStatusAsync_ClientCannotCancelAnotherClientsClaim()
    {
        var claim = TestData.NewClaim(CurrentVersion, ClaimStatus.Opened, TestData.OtherClientId);
        SetupClaimForUpdate(claim);

        var result = await _service.UpdateStatusAsync(TestData.ClientUser, claim.Id, StatusDto(ClaimStatus.Cancelled));

        Assert.Equal(ResultStatus.Forbidden, result.Status);
        Assert.Equal(ClaimStatus.Opened, claim.Status);
    }

    [Fact]
    public async Task UpdateStatusAsync_ClientSubmitsDraft_OpensIt()
    {
        var claim = TestData.NewClaim(CurrentVersion, ClaimStatus.Draft);
        SetupClaimForUpdate(claim);

        var result = await _service.UpdateStatusAsync(TestData.ClientUser, claim.Id, StatusDto(ClaimStatus.Opened));

        Assert.Equal(ResultStatus.Success, result.Status);
        Assert.Equal(ClaimStatus.Opened, claim.Status);
    }

    [Fact]
    public async Task UpdateStatusAsync_HandlingAppraiserApproves()
    {
        var claim = TestData.NewClaim(CurrentVersion, ClaimStatus.InTreatment, handlerId: TestData.AppraiserId);
        SetupClaimForUpdate(claim);

        var result = await _service.UpdateStatusAsync(TestData.AppraiserUser, claim.Id, StatusDto(ClaimStatus.Approved));

        Assert.Equal(ResultStatus.Success, result.Status);
        Assert.Equal(ClaimStatus.Approved, claim.Status);
    }

    [Fact]
    public async Task UpdateStatusAsync_AppraiserWhoDoesNotHandleTheClaim_IsForbidden()
    {
        var claim = TestData.NewClaim(CurrentVersion, ClaimStatus.InTreatment, handlerId: TestData.OtherAppraiserId);
        SetupClaimForUpdate(claim);

        var result = await _service.UpdateStatusAsync(TestData.AppraiserUser, claim.Id, StatusDto(ClaimStatus.Approved));

        Assert.Equal(ResultStatus.Forbidden, result.Status);
        Assert.Equal(ClaimStatus.InTreatment, claim.Status);
    }

    [Fact]
    public async Task DeleteAsync_ClientDeletesOwnDraft()
    {
        var claim = TestData.NewClaim(CurrentVersion, ClaimStatus.Draft);
        SetupClaimForUpdate(claim);

        var result = await _service.DeleteAsync(TestData.ClientUser, claim.Id);

        Assert.Equal(ResultStatus.Success, result.Status);
        _claims.Verify(c => c.Remove(claim), Times.Once);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_ClientCannotDeleteDraftOfAnotherClient()
    {
        var claim = TestData.NewClaim(CurrentVersion, ClaimStatus.Draft, TestData.OtherClientId);
        SetupClaimForUpdate(claim);

        var result = await _service.DeleteAsync(TestData.ClientUser, claim.Id);

        Assert.Equal(ResultStatus.Forbidden, result.Status);
        _claims.Verify(c => c.Remove(It.IsAny<Claim>()), Times.Never);
    }

    [Fact]
    public async Task DeleteAsync_ClientCannotDeleteClaimThatIsNotADraft()
    {
        var claim = TestData.NewClaim(CurrentVersion, ClaimStatus.Opened);
        SetupClaimForUpdate(claim);

        var result = await _service.DeleteAsync(TestData.ClientUser, claim.Id);

        Assert.Equal(ResultStatus.ValidationFailed, result.Status);
        _claims.Verify(c => c.Remove(It.IsAny<Claim>()), Times.Never);
    }

    [Theory]
    [InlineData(AppRoles.Manager)]
    [InlineData(AppRoles.Appraiser)]
    public async Task DeleteAsync_EmployeesCannotDeleteClaims(string role)
    {
        var claim = TestData.NewClaim(CurrentVersion, ClaimStatus.Draft);
        SetupClaimForUpdate(claim);

        var result = await _service.DeleteAsync(new CurrentUser(TestData.ManagerId, role), claim.Id);

        Assert.Equal(ResultStatus.Forbidden, result.Status);
        _claims.Verify(c => c.Remove(It.IsAny<Claim>()), Times.Never);
    }

    // ---------- take for handling ----------

    [Fact]
    public async Task TakeAsync_OpenClaimInSpecialization_AssignsHandlerAndCreatesReferralInOneSave()
    {
        var claim = TestData.NewClaim(CurrentVersion);
        SetupClaimForUpdate(claim);
        SetupAppraiser(TestData.NewAppraiser(TestData.AppraiserId, TestData.CarType()));

        var result = await _service.TakeAsync(TestData.AppraiserUser, claim.Id, new ClaimHandlerUpdateDto { Version = CurrentVersion });

        Assert.Equal(ResultStatus.Success, result.Status);
        Assert.Equal(TestData.AppraiserId, claim.HandlerId);
        Assert.Equal(ClaimStatus.InTreatment, claim.Status);
        Assert.Equal(Now.UtcDateTime, claim.AssignedAt);
        Assert.Equal("שמאי 2", result.Value!.HandlerName);
        _referrals.Verify(r => r.Add(It.Is<Referral>(referral =>
            referral.AppraiserId == TestData.AppraiserId &&
            referral.Claim == claim &&
            referral.ReferredAt == Now.UtcDateTime)), Times.Once);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task TakeAsync_ClaimAlreadyInTreatment_ReturnsConflictWithoutSaving()
    {
        var claim = TestData.NewClaim(CurrentVersion, ClaimStatus.InTreatment, handlerId: TestData.OtherAppraiserId);
        SetupClaimForUpdate(claim);
        SetupAppraiser(TestData.NewAppraiser(TestData.AppraiserId, TestData.CarType()));

        var result = await _service.TakeAsync(TestData.AppraiserUser, claim.Id, new ClaimHandlerUpdateDto { Version = CurrentVersion });

        Assert.Equal(ResultStatus.Conflict, result.Status);
        Assert.Equal(ErrorMessages.ClaimTakenByAnother, result.Error);
        Assert.Equal(TestData.OtherAppraiserId, claim.HandlerId);
        _referrals.Verify(r => r.Add(It.IsAny<Referral>()), Times.Never);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task TakeAsync_LosingTheRaceAtSaveTime_ReturnsConflict()
    {
        var claim = TestData.NewClaim(CurrentVersion);
        SetupClaimForUpdate(claim);
        SetupAppraiser(TestData.NewAppraiser(TestData.AppraiserId, TestData.CarType()));
        _unitOfWork
            .Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ConcurrencyConflictException("lost", new InvalidOperationException()));

        var result = await _service.TakeAsync(TestData.AppraiserUser, claim.Id, new ClaimHandlerUpdateDto { Version = CurrentVersion });

        Assert.Equal(ResultStatus.Conflict, result.Status);
        Assert.Equal(ErrorMessages.ClaimTakenByAnother, result.Error);
    }

    [Fact]
    public async Task TakeAsync_StaleVersion_ReturnsConflictWithoutSaving()
    {
        var claim = TestData.NewClaim(CurrentVersion);
        SetupClaimForUpdate(claim);
        SetupAppraiser(TestData.NewAppraiser(TestData.AppraiserId, TestData.CarType()));

        var result = await _service.TakeAsync(TestData.AppraiserUser, claim.Id, new ClaimHandlerUpdateDto { Version = Guid.NewGuid() });

        Assert.Equal(ResultStatus.Conflict, result.Status);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task TakeAsync_ClaimOutsideSpecialization_IsForbiddenWithoutSaving()
    {
        var claim = TestData.NewClaim(CurrentVersion); // car claim
        SetupClaimForUpdate(claim);
        SetupAppraiser(TestData.NewAppraiser(TestData.AppraiserId, TestData.HomeType())); // home specialist only

        var result = await _service.TakeAsync(TestData.AppraiserUser, claim.Id, new ClaimHandlerUpdateDto { Version = CurrentVersion });

        Assert.Equal(ResultStatus.Forbidden, result.Status);
        Assert.Null(claim.HandlerId);
        _referrals.Verify(r => r.Add(It.IsAny<Referral>()), Times.Never);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task TakeAsync_ClosedClaim_IsRejected()
    {
        var claim = TestData.NewClaim(CurrentVersion, ClaimStatus.Cancelled);
        SetupClaimForUpdate(claim);

        var result = await _service.TakeAsync(TestData.AppraiserUser, claim.Id, new ClaimHandlerUpdateDto { Version = CurrentVersion });

        Assert.Equal(ResultStatus.ValidationFailed, result.Status);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task TakeAsync_Draft_LooksLikeANonExistingClaim()
    {
        var claim = TestData.NewClaim(CurrentVersion, ClaimStatus.Draft);
        SetupClaimForUpdate(claim);

        var result = await _service.TakeAsync(TestData.AppraiserUser, claim.Id, new ClaimHandlerUpdateDto { Version = CurrentVersion });

        Assert.Equal(ResultStatus.NotFound, result.Status);
    }

    [Fact]
    public async Task TakeAsync_ManagerCannotTakeClaims()
    {
        var result = await _service.TakeAsync(TestData.ManagerUser, 100, new ClaimHandlerUpdateDto { Version = CurrentVersion });

        Assert.Equal(ResultStatus.Forbidden, result.Status);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    // ---------- who sees which claims ----------

    [Fact]
    public async Task GetPagedAsync_Client_IsRestrictedToOwnClaims()
    {
        SetupPage();

        await _service.GetPagedAsync(TestData.ClientUser, new ClaimQueryParameters());

        _claims.Verify(c => c.GetPagedAsync(It.IsAny<ClaimQueryParameters>(),
            It.Is<ClaimAccess>(a => a.ClientId == TestData.ClientId && a.AppraiserId == null),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetPagedAsync_Appraiser_IsRestrictedToOwnClaimsAndOwnSpecializations()
    {
        SetupPage();
        SetupAppraiser(TestData.NewAppraiser(TestData.AppraiserId, TestData.CarType(), TestData.HomeType()));

        await _service.GetPagedAsync(TestData.AppraiserUser, new ClaimQueryParameters());

        _claims.Verify(c => c.GetPagedAsync(It.IsAny<ClaimQueryParameters>(),
            It.Is<ClaimAccess>(a => a.ClientId == null && a.AppraiserId == TestData.AppraiserId &&
                                    a.SpecializationIds.SequenceEqual(new[] { TestData.CarTypeId, TestData.HomeTypeId })),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetByIdAsync_AnotherClientsClaim_LooksLikeANonExistingClaim()
    {
        _claims.Setup(c => c.GetByIdAsync(100, It.IsAny<CancellationToken>()))
            .ReturnsAsync(TestData.NewClaim(CurrentVersion, ClaimStatus.Opened, TestData.OtherClientId));

        var result = await _service.GetByIdAsync(TestData.ClientUser, 100);

        Assert.Equal(ResultStatus.NotFound, result.Status);
    }

    [Fact]
    public async Task GetByIdAsync_ManagerDoesNotSeeDrafts()
    {
        _claims.Setup(c => c.GetByIdAsync(100, It.IsAny<CancellationToken>()))
            .ReturnsAsync(TestData.NewClaim(CurrentVersion, ClaimStatus.Draft));

        var result = await _service.GetByIdAsync(TestData.ManagerUser, 100);

        Assert.Equal(ResultStatus.NotFound, result.Status);
    }

    [Fact]
    public async Task GetByIdAsync_AppraiserDoesNotSeeClaimHandledByAnotherAppraiser()
    {
        _claims.Setup(c => c.GetByIdAsync(100, It.IsAny<CancellationToken>()))
            .ReturnsAsync(TestData.NewClaim(CurrentVersion, ClaimStatus.InTreatment, handlerId: TestData.OtherAppraiserId));

        var result = await _service.GetByIdAsync(TestData.AppraiserUser, 100);

        Assert.Equal(ResultStatus.NotFound, result.Status);
    }

    // ---------- helpers ----------

    private static ClaimCreateDto NewClaimDto() => new()
    {
        InsuranceTypeId = TestData.CarTypeId,
        Description = "פגיעה ברכב בחניה",
        Amount = 3200m
    };

    private static ClaimStatusUpdateDto StatusDto(ClaimStatus status) => new() { Status = status, Version = CurrentVersion };

    private void SetupClient(Client client) =>
        _clients.Setup(c => c.GetByIdAsync(client.Id, It.IsAny<CancellationToken>())).ReturnsAsync(client);

    private void SetupAppraiser(Employee appraiser) =>
        _employees.Setup(e => e.GetByIdAsync(appraiser.Id, It.IsAny<CancellationToken>())).ReturnsAsync(appraiser);

    private void SetupClaimForUpdate(Claim claim) =>
        _claims.Setup(c => c.GetForUpdateAsync(claim.Id, It.IsAny<CancellationToken>())).ReturnsAsync(claim);

    private void SetupPage() =>
        _claims.Setup(c => c.GetPagedAsync(It.IsAny<ClaimQueryParameters>(), It.IsAny<ClaimAccess>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((new List<Claim>(), 0));
}
