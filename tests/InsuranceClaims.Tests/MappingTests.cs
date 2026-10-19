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
        var responseTypes = new[] { typeof(EmployeeDto), typeof(AuthResponseDto), typeof(ClaimDto), typeof(PolicyDto), typeof(InsuranceTypeDto) };

        foreach (var type in responseTypes)
        {
            Assert.DoesNotContain(type.GetProperties(), p => p.Name.Contains("Password", StringComparison.OrdinalIgnoreCase));
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
            Username = "adj",
            PasswordHash = "secret-hash",
            Role = EmployeeRole.Adjuster,
            Specializations = { new InsuranceType { Id = 1, Name = "ביטוח רכב" } }
        };

        var dto = mapper.Map<EmployeeDto>(employee);

        Assert.Equal(7, dto.Id);
        Assert.Equal(EmployeeRole.Adjuster, dto.Role);
        Assert.Single(dto.Specializations);
    }

    [Fact]
    public void ClaimUpdateDto_DoesNotOverwriteVersion()
    {
        var mapper = TestMapper.Create();
        var version = Guid.NewGuid();
        var claim = new Claim { Id = 1, Title = "old", Description = "old", Amount = 1m, Version = version };

        mapper.Map(new ClaimUpdateDto { Title = "new", Description = "new", Amount = 2m, Version = Guid.NewGuid() }, claim);

        Assert.Equal("new", claim.Title);
        Assert.Equal(version, claim.Version);
    }

    [Fact]
    public void PolicyCreateDto_DatesBecomeUtc()
    {
        var mapper = TestMapper.Create();

        var policy = mapper.Map<Policy>(new PolicyCreateDto
        {
            PolicyNumber = "P-1",
            HolderName = "בעל פוליסה",
            CoverageAmount = 10m,
            InsuranceTypeId = 1,
            StartDate = new DateTime(2026, 1, 1),
            EndDate = new DateTime(2027, 1, 1)
        });

        Assert.Equal(DateTimeKind.Utc, policy.StartDate.Kind);
        Assert.Equal(DateTimeKind.Utc, policy.EndDate.Kind);
    }
}
