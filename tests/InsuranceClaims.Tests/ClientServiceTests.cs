using InsuranceClaims.Core.Common;
using InsuranceClaims.Core.DTOs;
using InsuranceClaims.Core.Entities;
using InsuranceClaims.Core.Interfaces.Repositories;
using InsuranceClaims.Service.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace InsuranceClaims.Tests;

public class ClientServiceTests
{
    private readonly Mock<IClientRepository> _clients = new();
    private readonly Mock<IInsuranceTypeRepository> _insuranceTypes = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly ClientService _service;

    public ClientServiceTests()
    {
        _service = new ClientService(
            _clients.Object,
            _insuranceTypes.Object,
            _unitOfWork.Object,
            TestMapper.Create(),
            NullLogger<ClientService>.Instance);
    }

    [Fact]
    public async Task UpdateAsync_ClientUpdatesOwnDetails()
    {
        var client = TestData.NewClient();
        _clients.Setup(c => c.GetForUpdateAsync(TestData.ClientId, It.IsAny<CancellationToken>())).ReturnsAsync(client);

        var result = await _service.UpdateAsync(TestData.ClientUser, TestData.ClientId,
            new ClientUpdateDto { FullName = "שם חדש", Phone = "0521112233", Address = "כתובת חדשה 5" });

        Assert.Equal(ResultStatus.Success, result.Status);
        Assert.Equal("שם חדש", client.FullName);
        Assert.Equal("0521112233", client.Phone);
        Assert.Equal("client@example.com", client.Email); // not editable
        Assert.Equal("123456789", client.NationalId);     // not editable
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_ClientCannotUpdateAnotherClient()
    {
        var result = await _service.UpdateAsync(TestData.ClientUser, TestData.OtherClientId,
            new ClientUpdateDto { FullName = "שם חדש", Phone = "0521112233", Address = "כתובת חדשה 5" });

        Assert.Equal(ResultStatus.Forbidden, result.Status);
        _clients.Verify(c => c.GetForUpdateAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Theory]
    [InlineData(AppRoles.Manager)]
    [InlineData(AppRoles.Appraiser)]
    public async Task UpdateAsync_EmployeesCannotUpdateClientDetails(string role)
    {
        var result = await _service.UpdateAsync(new CurrentUser(TestData.ClientId, role), TestData.ClientId,
            new ClientUpdateDto { FullName = "שם חדש", Phone = "0521112233", Address = "כתובת חדשה 5" });

        Assert.Equal(ResultStatus.Forbidden, result.Status);
    }

    [Fact]
    public async Task GetPagedAsync_ClientCannotListClients()
    {
        var result = await _service.GetPagedAsync(TestData.ClientUser, new PagingParameters());

        Assert.Equal(ResultStatus.Forbidden, result.Status);
    }

    [Fact]
    public async Task GetPagedAsync_ManagerSeesAllClients()
    {
        _clients.Setup(c => c.GetPagedAsync(It.IsAny<PagingParameters>(), null, It.IsAny<CancellationToken>()))
            .ReturnsAsync((new List<Client> { TestData.NewClient() }, 1));

        var result = await _service.GetPagedAsync(TestData.ManagerUser, new PagingParameters());

        Assert.Equal(ResultStatus.Success, result.Status);
        Assert.Equal("123456789", result.Value!.Items.Single().NationalId);
    }

    [Fact]
    public async Task GetPagedAsync_AppraiserGetsOnlyHisClientsWithoutNationalId()
    {
        _clients.Setup(c => c.GetPagedAsync(It.IsAny<PagingParameters>(), TestData.AppraiserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((new List<Client> { TestData.NewClient() }, 1));

        var result = await _service.GetPagedAsync(TestData.AppraiserUser, new PagingParameters());

        Assert.Equal(ResultStatus.Success, result.Status);
        var dto = result.Value!.Items.Single();
        Assert.Equal("0501234567", dto.Phone);
        Assert.Null(dto.NationalId);
    }

    [Fact]
    public async Task GetByIdAsync_AppraiserWithActiveClaim_GetsContactDetails()
    {
        _clients.Setup(c => c.GetByIdAsync(TestData.ClientId, It.IsAny<CancellationToken>())).ReturnsAsync(TestData.NewClient());
        _clients.Setup(c => c.HasActiveRelationWithAppraiserAsync(TestData.ClientId, TestData.AppraiserId, It.IsAny<CancellationToken>())).ReturnsAsync(true);

        var result = await _service.GetByIdAsync(TestData.AppraiserUser, TestData.ClientId);

        Assert.Equal(ResultStatus.Success, result.Status);
        Assert.Equal("0501234567", result.Value!.Phone);
        Assert.Null(result.Value.NationalId);
    }

    [Fact]
    public async Task GetByIdAsync_AppraiserWithoutActiveClaimOrReferral_IsForbidden()
    {
        _clients.Setup(c => c.GetByIdAsync(TestData.ClientId, It.IsAny<CancellationToken>())).ReturnsAsync(TestData.NewClient());
        _clients.Setup(c => c.HasActiveRelationWithAppraiserAsync(TestData.ClientId, TestData.AppraiserId, It.IsAny<CancellationToken>())).ReturnsAsync(false);

        var result = await _service.GetByIdAsync(TestData.AppraiserUser, TestData.ClientId);

        Assert.Equal(ResultStatus.Forbidden, result.Status);
        Assert.Null(result.Value);
    }

    [Fact]
    public async Task GetByIdAsync_ClientCannotReadAnotherClient()
    {
        _clients.Setup(c => c.GetByIdAsync(TestData.OtherClientId, It.IsAny<CancellationToken>())).ReturnsAsync(TestData.NewClient(TestData.OtherClientId));

        var result = await _service.GetByIdAsync(TestData.ClientUser, TestData.OtherClientId);

        Assert.Equal(ResultStatus.Forbidden, result.Status);
    }

    [Fact]
    public async Task UpdateInsuranceTypesAsync_OnlyManagerMay()
    {
        var result = await _service.UpdateInsuranceTypesAsync(TestData.ClientUser, TestData.ClientId,
            new ClientInsuranceTypesUpdateDto { InsuranceTypeIds = { TestData.CarTypeId } });

        Assert.Equal(ResultStatus.Forbidden, result.Status);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
}
