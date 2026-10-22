using InsuranceClaims.API.Security;
using InsuranceClaims.Core.DTOs;
using InsuranceClaims.Core.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace InsuranceClaims.API.Controllers;

[Route("api/clients")]
[Authorize]
public class ClientsController : ApiControllerBase
{
    private readonly IClientService _clientService;

    public ClientsController(IClientService clientService)
    {
        _clientService = clientService;
    }

    /// <summary>Manager: all clients. Appraiser: only the contact details of clients with an active claim or referral at them.</summary>
    [HttpGet]
    [Authorize(Roles = Roles.ManagerOrAppraiser)]
    [ProducesResponseType(typeof(PagedResult<ClientDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<ClientDto>>> GetPaged([FromQuery] PagingParameters paging, CancellationToken cancellationToken) =>
        OkOrError(await _clientService.GetPagedAsync(CurrentUser, paging, cancellationToken));

    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(ClientDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<ClientDto>> GetById(int id, CancellationToken cancellationToken) =>
        OkOrError(await _clientService.GetByIdAsync(CurrentUser, id, cancellationToken));

    /// <summary>A client updates their own details (name, phone, address).</summary>
    [HttpPut("{id:int}")]
    [Authorize(Roles = Roles.Client)]
    [ProducesResponseType(typeof(ClientDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<ClientDto>> Update(int id, ClientUpdateDto dto, CancellationToken cancellationToken) =>
        OkOrError(await _clientService.UpdateAsync(CurrentUser, id, dto, cancellationToken));

    /// <summary>Manager sets the insurance types a client holds.</summary>
    [HttpPut("{id:int}/insurance-types")]
    [Authorize(Roles = Roles.Manager)]
    [ProducesResponseType(typeof(ClientDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<ClientDto>> UpdateInsuranceTypes(int id, ClientInsuranceTypesUpdateDto dto, CancellationToken cancellationToken) =>
        OkOrError(await _clientService.UpdateInsuranceTypesAsync(CurrentUser, id, dto, cancellationToken));
}
