using InsuranceClaims.Core.DTOs;
using InsuranceClaims.Core.Entities;
using InsuranceClaims.Core.Enums;

namespace InsuranceClaims.Tests;

public class MappingTests
{
    [Fact]
    public void MappingConfiguration_IsValid()
    {
        TestMapper.CreateConfiguration().AssertConfigurationIsValid();
    }

    [Fact]
    public void ResponseDtos_NeverExposePasswords()
    {
        var responseTypes = new[]
        {
            typeof(EmployeeDto), typeof(ClientDto), typeof(UserDto), typeof(AuthResponseDto),
            typeof(ClaimDto), typeof(ReferralDto), typeof(InsuranceTypeDto)
        };

        foreach (var type in responseTypes)
        {
            Assert.DoesNotContain(type.GetProperties(), p => p.Name.Contains("Password", StringComparison.OrdinalIgnoreCase));
        }
    }

    [Fact]
    public void ResponseDtos_AreNotEntities()
    {
        var entityTypes = typeof(Claim).Assembly.GetTypes().Where(t => t.Namespace == typeof(Claim).Namespace).ToHashSet();
        var responseTypes = new[] { typeof(EmployeeDto), typeof(ClientDto), typeof(ClaimDto), typeof(ReferralDto), typeof(AuthResponseDto) };

        foreach (var type in responseTypes)
        {
            Assert.DoesNotContain(type.GetProperties(), p => entityTypes.Contains(p.PropertyType));
        }
    }

    [Fact]
    public void Employee_MapsToDto_WithSpecializations()
    {
        var mapper = TestMapper.Create();
        var employee = new Employee
        {
            Id = 7,
            FullName = "שמאי",
            Email = "appraiser@example.com",
            PasswordHash = "secret-hash",
            Role = EmployeeRole.Appraiser,
            Specializations = { new InsuranceType { Id = 1, Name = "ביטוח רכב" } }
        };

        var dto = mapper.Map<EmployeeDto>(employee);

        Assert.Equal(7, dto.Id);
        Assert.Equal(EmployeeRole.Appraiser, dto.Role);
        Assert.Single(dto.Specializations);
    }

    [Fact]
    public void UserDto_CarriesTheRoleName()
    {
        var mapper = TestMapper.Create();

        var employeeUser = mapper.Map<UserDto>(new Employee { Id = 1, FullName = "מנהל", Email = "m@example.com", Role = EmployeeRole.Manager });
        var clientUser = mapper.Map<UserDto>(new Client { Id = 2, FullName = "לקוח", Email = "c@example.com" });

        Assert.Equal("Manager", employeeUser.Role);
        Assert.Equal("Client", clientUser.Role);
    }

    [Fact]
    public void Client_MapsToDto_WithInsuranceTypes()
    {
        var mapper = TestMapper.Create();
        var client = TestData.NewClient(5, TestData.CarType(), TestData.HomeType());

        var dto = mapper.Map<ClientDto>(client);

        Assert.Equal(2, dto.InsuranceTypes.Count);
        Assert.Equal("client@example.com", dto.Email);
    }

    [Fact]
    public void EmployeeCreateDto_DoesNotMapThePasswordOrRole()
    {
        var mapper = TestMapper.Create();

        var employee = mapper.Map<Employee>(new EmployeeCreateDto
        {
            FullName = "עובד",
            Email = "e@example.com",
            Password = "Secret123!",
            Role = EmployeeRole.Appraiser
        });

        Assert.Equal(string.Empty, employee.PasswordHash);
        Assert.Equal(EmployeeRole.Manager, employee.Role); // default - the service sets the role explicitly
    }

    [Fact]
    public void ClaimUpdateDto_DoesNotOverwriteVersion()
    {
        var mapper = TestMapper.Create();
        var version = Guid.NewGuid();
        var claim = new Claim { Id = 1, Description = "old", Amount = 1m, Version = version };

        mapper.Map(new ClaimUpdateDto { InsuranceTypeId = 1, Description = "new", Amount = 2m, Version = Guid.NewGuid() }, claim);

        Assert.Equal("new", claim.Description);
        Assert.Equal(version, claim.Version);
    }

    [Fact]
    public void Referral_MapsClaimClientAndAppraiserDetails()
    {
        var mapper = TestMapper.Create();
        var referral = new Referral
        {
            Id = 3,
            ClaimId = 100,
            AppraiserId = 2,
            Appraiser = TestData.NewAppraiser(2),
            Claim = TestData.NewClaim(Guid.NewGuid(), ClaimStatus.InTreatment, handlerId: 2),
            ReferredAt = new DateTime(2026, 9, 5, 9, 0, 0, DateTimeKind.Utc)
        };

        var dto = mapper.Map<ReferralDto>(referral);

        Assert.Equal("CLM-0100", dto.ClaimNumber);
        Assert.Equal(ClaimStatus.InTreatment, dto.ClaimStatus);
        Assert.Equal("שמאי 2", dto.AppraiserName);
        Assert.Equal("ביטוח רכב", dto.InsuranceTypeName);
    }
}
