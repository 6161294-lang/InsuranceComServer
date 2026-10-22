using InsuranceClaims.Core.Common;
using InsuranceClaims.Core.DTOs;
using InsuranceClaims.Core.Entities;
using InsuranceClaims.Core.Enums;
using InsuranceClaims.Core.Interfaces.Repositories;
using InsuranceClaims.Core.Interfaces.Services;
using InsuranceClaims.Service.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace InsuranceClaims.Tests;

/// <summary>Role rules of the services that are not covered by the claim and client tests.</summary>
public class RoleAuthorizationTests
{
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<IEmployeeRepository> _employees = new();
    private readonly Mock<IClientRepository> _clients = new();
    private readonly Mock<IInsuranceTypeRepository> _insuranceTypes = new();
    private readonly Mock<IReferralRepository> _referrals = new();
    private readonly Mock<IPasswordHasher> _passwordHasher = new();
    private readonly Mock<ITokenGenerator> _tokens = new();

    private EmployeeService CreateEmployeeService() => new(
        _employees.Object, _clients.Object, _insuranceTypes.Object, _unitOfWork.Object,
        _passwordHasher.Object, TestMapper.Create(), NullLogger<EmployeeService>.Instance);

    private ReferralService CreateReferralService() => new(
        _referrals.Object, _unitOfWork.Object, TestMapper.Create(), NullLogger<ReferralService>.Instance);

    private InsuranceTypeService CreateInsuranceTypeService() => new(
        _insuranceTypes.Object, _unitOfWork.Object, TestMapper.Create(), NullLogger<InsuranceTypeService>.Instance);

    private AuthService CreateAuthService() => new(
        _employees.Object, _clients.Object, _insuranceTypes.Object, _unitOfWork.Object,
        _passwordHasher.Object, _tokens.Object, TestMapper.Create(), NullLogger<AuthService>.Instance);

    // ---------- employees: created and deleted by a manager only ----------

    [Fact]
    public async Task CreateEmployee_ManagerCreatesAppraiserWithHashedPassword()
    {
        _passwordHasher.Setup(h => h.Hash("Secret123!")).Returns("HASHED");
        _insuranceTypes.Setup(t => t.GetByIdsAsync(It.IsAny<IEnumerable<int>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<InsuranceType> { TestData.CarType() });

        var result = await CreateEmployeeService().CreateAsync(TestData.ManagerUser, new EmployeeCreateDto
        {
            FullName = "שמאי חדש",
            Email = "  New.Appraiser@Example.com ",
            Password = "Secret123!",
            Role = EmployeeRole.Appraiser,
            SpecializationIds = { TestData.CarTypeId }
        });

        Assert.Equal(ResultStatus.Success, result.Status);
        _employees.Verify(e => e.Add(It.Is<Employee>(employee =>
            employee.PasswordHash == "HASHED" &&
            employee.Email == "new.appraiser@example.com" &&
            employee.Role == EmployeeRole.Appraiser &&
            employee.Specializations.Count == 1)), Times.Once);
        Assert.DoesNotContain(typeof(EmployeeDto).GetProperties(), p => p.Name.Contains("Password", StringComparison.OrdinalIgnoreCase));
    }

    [Theory]
    [InlineData(AppRoles.Client)]
    [InlineData(AppRoles.Appraiser)]
    public async Task CreateEmployee_NonManagerIsForbidden(string role)
    {
        var result = await CreateEmployeeService().CreateAsync(new CurrentUser(5, role), new EmployeeCreateDto
        {
            FullName = "עובד",
            Email = "worker@example.com",
            Password = "Secret123!",
            Role = EmployeeRole.Manager
        });

        Assert.Equal(ResultStatus.Forbidden, result.Status);
        _employees.Verify(e => e.Add(It.IsAny<Employee>()), Times.Never);
    }

    [Fact]
    public async Task DeleteEmployee_ManagerDeletesEmployeeWithoutHistory()
    {
        var employee = TestData.NewAppraiser(7, TestData.CarType());
        _employees.Setup(e => e.GetForUpdateAsync(7, It.IsAny<CancellationToken>())).ReturnsAsync(employee);

        var result = await CreateEmployeeService().DeleteAsync(TestData.ManagerUser, 7);

        Assert.Equal(ResultStatus.Success, result.Status);
        _employees.Verify(e => e.Remove(employee), Times.Once);
    }

    [Theory]
    [InlineData(AppRoles.Client)]
    [InlineData(AppRoles.Appraiser)]
    public async Task DeleteEmployee_NonManagerIsForbidden(string role)
    {
        var result = await CreateEmployeeService().DeleteAsync(new CurrentUser(5, role), 7);

        Assert.Equal(ResultStatus.Forbidden, result.Status);
        _employees.Verify(e => e.Remove(It.IsAny<Employee>()), Times.Never);
    }

    [Fact]
    public async Task DeleteEmployee_ManagerCannotDeleteHimself()
    {
        var result = await CreateEmployeeService().DeleteAsync(TestData.ManagerUser, TestData.ManagerId);

        Assert.Equal(ResultStatus.ValidationFailed, result.Status);
        _employees.Verify(e => e.Remove(It.IsAny<Employee>()), Times.Never);
    }

    [Fact]
    public async Task DeleteEmployee_EmployeeWithWorkHistoryIsKept()
    {
        _employees.Setup(e => e.GetForUpdateAsync(7, It.IsAny<CancellationToken>())).ReturnsAsync(TestData.NewAppraiser(7));
        _employees.Setup(e => e.HasWorkHistoryAsync(7, It.IsAny<CancellationToken>())).ReturnsAsync(true);

        var result = await CreateEmployeeService().DeleteAsync(TestData.ManagerUser, 7);

        Assert.Equal(ResultStatus.Conflict, result.Status);
        _employees.Verify(e => e.Remove(It.IsAny<Employee>()), Times.Never);
    }

    // ---------- referrals: only the appraiser they belong to ----------

    [Fact]
    public async Task GetReferral_AppraiserDoesNotSeeReferralOfAnotherAppraiser()
    {
        _referrals.Setup(r => r.GetByIdAsync(50, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Referral { Id = 50, ClaimId = 100, AppraiserId = TestData.OtherAppraiserId });

        var result = await CreateReferralService().GetByIdAsync(TestData.AppraiserUser, 50);

        Assert.Equal(ResultStatus.Forbidden, result.Status);
        Assert.Null(result.Value);
    }

    [Fact]
    public async Task GetReferral_AppraiserSeesOwnReferral()
    {
        _referrals.Setup(r => r.GetByIdAsync(50, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Referral
            {
                Id = 50,
                ClaimId = 100,
                AppraiserId = TestData.AppraiserId,
                Appraiser = TestData.NewAppraiser(TestData.AppraiserId),
                Claim = TestData.NewClaim(Guid.NewGuid(), ClaimStatus.InTreatment, handlerId: TestData.AppraiserId)
            });

        var result = await CreateReferralService().GetByIdAsync(TestData.AppraiserUser, 50);

        Assert.Equal(ResultStatus.Success, result.Status);
        Assert.Equal("CLM-0100", result.Value!.ClaimNumber);
        Assert.Equal("לקוח לדוגמה", result.Value.ClientName);
    }

    [Fact]
    public async Task UpdateReferral_AppraiserCannotChangeReferralOfAnotherAppraiser()
    {
        _referrals.Setup(r => r.GetForUpdateAsync(50, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Referral { Id = 50, ClaimId = 100, AppraiserId = TestData.OtherAppraiserId });

        var result = await CreateReferralService().UpdateAsync(TestData.AppraiserUser, 50, new ReferralUpdateDto { Notes = "הערה" });

        Assert.Equal(ResultStatus.Forbidden, result.Status);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Theory]
    [InlineData(AppRoles.Manager)]
    [InlineData(AppRoles.Client)]
    public async Task GetReferrals_OnlyAppraisersMaySeeReferrals(string role)
    {
        var result = await CreateReferralService().GetPagedAsync(new CurrentUser(5, role), new PagingParameters());

        Assert.Equal(ResultStatus.Forbidden, result.Status);
        _referrals.Verify(r => r.GetPagedByAppraiserAsync(It.IsAny<int>(), It.IsAny<PagingParameters>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    // ---------- insurance types: managed by a manager ----------

    [Theory]
    [InlineData(AppRoles.Client)]
    [InlineData(AppRoles.Appraiser)]
    public async Task CreateInsuranceType_NonManagerIsForbidden(string role)
    {
        var result = await CreateInsuranceTypeService().CreateAsync(new CurrentUser(5, role),
            new InsuranceTypeCreateDto { Name = "ביטוח חיים" });

        Assert.Equal(ResultStatus.Forbidden, result.Status);
        _insuranceTypes.Verify(t => t.Add(It.IsAny<InsuranceType>()), Times.Never);
    }

    [Fact]
    public async Task CreateInsuranceType_ManagerCreates()
    {
        var result = await CreateInsuranceTypeService().CreateAsync(TestData.ManagerUser,
            new InsuranceTypeCreateDto { Name = "ביטוח חיים" });

        Assert.Equal(ResultStatus.Success, result.Status);
        _insuranceTypes.Verify(t => t.Add(It.Is<InsuranceType>(type => type.Name == "ביטוח חיים")), Times.Once);
    }

    // ---------- registration and login ----------

    [Fact]
    public async Task Register_StoresOnlyTheHashOfThePassword()
    {
        _passwordHasher.Setup(h => h.Hash("Secret123!")).Returns("HASHED");
        _insuranceTypes.Setup(t => t.GetByIdsAsync(It.IsAny<IEnumerable<int>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<InsuranceType> { TestData.CarType() });

        var result = await CreateAuthService().RegisterClientAsync(new RegisterClientDto
        {
            FullName = "לקוח חדש",
            Email = "New@Example.com",
            Password = "Secret123!",
            Phone = "0501234567",
            Address = "רחוב 1",
            NationalId = "123456789",
            InsuranceTypeIds = { TestData.CarTypeId }
        });

        Assert.Equal(ResultStatus.Success, result.Status);
        _clients.Verify(c => c.Add(It.Is<Client>(client =>
            client.PasswordHash == "HASHED" &&
            client.Email == "new@example.com" &&
            client.InsuranceTypes.Count == 1)), Times.Once);
        Assert.DoesNotContain(typeof(ClientDto).GetProperties(), p => p.Name.Contains("Password", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task Register_EmailAlreadyUsedByAnEmployee_IsConflict()
    {
        _employees.Setup(e => e.EmailExistsAsync("manager@example.com", It.IsAny<CancellationToken>())).ReturnsAsync(true);

        var result = await CreateAuthService().RegisterClientAsync(new RegisterClientDto
        {
            FullName = "לקוח",
            Email = "manager@example.com",
            Password = "Secret123!",
            Phone = "0501234567",
            Address = "רחוב 1",
            NationalId = "123456789",
            InsuranceTypeIds = { TestData.CarTypeId }
        });

        Assert.Equal(ResultStatus.Conflict, result.Status);
        _clients.Verify(c => c.Add(It.IsAny<Client>()), Times.Never);
    }

    [Fact]
    public async Task Login_WrongPassword_IsUnauthorized()
    {
        _clients.Setup(c => c.GetByEmailAsync("client@example.com", It.IsAny<CancellationToken>())).ReturnsAsync(TestData.NewClient());
        _passwordHasher.Setup(h => h.Verify("wrong", "hash")).Returns(false);

        var result = await CreateAuthService().LoginAsync(new LoginDto { Email = "client@example.com", Password = "wrong" });

        Assert.Equal(ResultStatus.Unauthorized, result.Status);
        _tokens.Verify(t => t.GenerateToken(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task Login_Client_GetsTokenWithClientRole()
    {
        _clients.Setup(c => c.GetByEmailAsync("client@example.com", It.IsAny<CancellationToken>())).ReturnsAsync(TestData.NewClient());
        _passwordHasher.Setup(h => h.Verify("right", "hash")).Returns(true);
        _tokens.Setup(t => t.GenerateToken(TestData.ClientId, "לקוח לדוגמה", "client@example.com", AppRoles.Client))
            .Returns(("TOKEN", DateTime.UtcNow.AddHours(1)));

        var result = await CreateAuthService().LoginAsync(new LoginDto { Email = "Client@Example.com", Password = "right" });

        Assert.Equal(ResultStatus.Success, result.Status);
        Assert.Equal("TOKEN", result.Value!.Token);
        Assert.Equal(AppRoles.Client, result.Value.User.Role);
    }

    [Fact]
    public async Task Login_Employee_GetsTokenWithEmployeeRole()
    {
        var appraiser = TestData.NewAppraiser(TestData.AppraiserId, TestData.CarType());
        _employees.Setup(e => e.GetByEmailAsync(appraiser.Email, It.IsAny<CancellationToken>())).ReturnsAsync(appraiser);
        _passwordHasher.Setup(h => h.Verify("right", "hash")).Returns(true);
        _tokens.Setup(t => t.GenerateToken(appraiser.Id, appraiser.FullName, appraiser.Email, AppRoles.Appraiser))
            .Returns(("TOKEN", DateTime.UtcNow.AddHours(1)));

        var result = await CreateAuthService().LoginAsync(new LoginDto { Email = appraiser.Email, Password = "right" });

        Assert.Equal(ResultStatus.Success, result.Status);
        Assert.Equal(AppRoles.Appraiser, result.Value!.User.Role);
    }
}
