using InsuranceClaims.API.Models;
using InsuranceClaims.API.Security;
using InsuranceClaims.Core.DTOs;
using InsuranceClaims.Core.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace InsuranceClaims.API.Controllers;

[Route("api/claims")]
[Authorize]
public class ClaimsController : ApiControllerBase
{
    private readonly IClaimService _claimService;

    public ClaimsController(IClaimService claimService)
    {
        _claimService = claimService;
    }

    /// <summary>Claims, filtered, sorted and paged on the server (e.g. ?status=Open&amp;page=1&amp;pageSize=10).</summary>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<ClaimDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<PagedResult<ClaimDto>>> GetPaged([FromQuery] ClaimQueryParameters query, CancellationToken cancellationToken) =>
        Ok(await _claimService.GetPagedAsync(query, cancellationToken));

    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(ClaimDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ClaimDto>> GetById(int id, CancellationToken cancellationToken) =>
        OkOrError(await _claimService.GetByIdAsync(id, cancellationToken));

    /// <summary>Opens a claim. Allowed only on a policy that is currently in force.</summary>
    [HttpPost]
    [ProducesResponseType(typeof(ClaimDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ClaimDto>> Create(ClaimCreateDto dto, CancellationToken cancellationToken)
    {
        var result = await _claimService.CreateAsync(dto, cancellationToken);
        return result.IsSuccess
            ? CreatedAtAction(nameof(GetById), new { id = result.Value!.Id }, result.Value)
            : Error(result);
    }

    /// <summary>Updates claim details (manager only). Returns the claim with its new version.</summary>
    [HttpPut("{id:int}")]
    [Authorize(Roles = Roles.Manager)]
    [ProducesResponseType(typeof(ClaimDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ClaimDto>> Update(int id, ClaimUpdateDto dto, CancellationToken cancellationToken) =>
        OkOrError(await _claimService.UpdateAsync(id, dto, cancellationToken));

    /// <summary>
    /// "Take for handling": sets the claim handler to the authenticated adjuster.
    /// Returns 409 when another employee already took the claim.
    /// </summary>
    [HttpPut("{id:int}/handler")]
    [Authorize(Roles = Roles.Adjuster)]
    [ProducesResponseType(typeof(ClaimDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ClaimDto>> UpdateHandler(int id, ClaimHandlerUpdateDto dto, CancellationToken cancellationToken) =>
        OkOrError(await _claimService.TakeAsync(id, CurrentEmployeeId, dto, cancellationToken));

    /// <summary>Changes the claim status. Only the current handler may do this. Status "Open" releases the claim.</summary>
    [HttpPut("{id:int}/status")]
    [Authorize(Roles = Roles.Adjuster)]
    [ProducesResponseType(typeof(ClaimDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ClaimDto>> UpdateStatus(int id, ClaimStatusUpdateDto dto, CancellationToken cancellationToken) =>
        OkOrError(await _claimService.UpdateStatusAsync(id, CurrentEmployeeId, dto, cancellationToken));

    /// <summary>Deletes a claim (manager only).</summary>
    [HttpDelete("{id:int}")]
    [Authorize(Roles = Roles.Manager)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken) =>
        NoContentOrError(await _claimService.DeleteAsync(id, cancellationToken));
}
