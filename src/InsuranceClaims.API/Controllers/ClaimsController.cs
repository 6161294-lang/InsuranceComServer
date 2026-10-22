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

    /// <summary>
    /// Claims the caller may see, filtered, sorted and paged on the server (e.g. ?status=Opened&amp;page=1&amp;pageSize=10).
    /// Client: own claims. Manager: all claims except drafts. Appraiser: own claims and open claims in their specializations
    /// (use scope=Mine or scope=Available to split them).
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<ClaimDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<ClaimDto>>> GetPaged([FromQuery] ClaimQueryParameters query, CancellationToken cancellationToken) =>
        OkOrError(await _claimService.GetPagedAsync(CurrentUser, query, cancellationToken));

    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(ClaimDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<ClaimDto>> GetById(int id, CancellationToken cancellationToken) =>
        OkOrError(await _claimService.GetByIdAsync(CurrentUser, id, cancellationToken));

    /// <summary>Opens a claim (client only) on an insurance type the client holds. Use saveAsDraft to keep it private.</summary>
    [HttpPost]
    [Authorize(Roles = Roles.Client)]
    [ProducesResponseType(typeof(ClaimDto), StatusCodes.Status201Created)]
    public async Task<ActionResult<ClaimDto>> Create(ClaimCreateDto dto, CancellationToken cancellationToken)
    {
        var result = await _claimService.CreateAsync(CurrentUser, dto, cancellationToken);
        return result.IsSuccess
            ? CreatedAtAction(nameof(GetById), new { id = result.Value!.Id }, result.Value)
            : Error(result);
    }

    /// <summary>The owning client edits a draft claim.</summary>
    [HttpPut("{id:int}")]
    [Authorize(Roles = Roles.Client)]
    [ProducesResponseType(typeof(ClaimDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<ClaimDto>> Update(int id, ClaimUpdateDto dto, CancellationToken cancellationToken) =>
        OkOrError(await _claimService.UpdateAsync(CurrentUser, id, dto, cancellationToken));

    /// <summary>
    /// "Take for handling": sets the claim handler to the authenticated appraiser and creates the referral.
    /// Returns 409 when another appraiser already took the claim.
    /// </summary>
    [HttpPut("{id:int}/handler")]
    [Authorize(Roles = Roles.Appraiser)]
    [ProducesResponseType(typeof(ClaimDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<ClaimDto>> UpdateHandler(int id, ClaimHandlerUpdateDto dto, CancellationToken cancellationToken) =>
        OkOrError(await _claimService.TakeAsync(CurrentUser, id, dto, cancellationToken));

    /// <summary>
    /// Changes the claim status. Client: submit a draft (Opened) or cancel (Cancelled).
    /// Handling appraiser: Approved or Rejected.
    /// </summary>
    [HttpPut("{id:int}/status")]
    [Authorize(Roles = Roles.ClientOrAppraiser)]
    [ProducesResponseType(typeof(ClaimDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<ClaimDto>> UpdateStatus(int id, ClaimStatusUpdateDto dto, CancellationToken cancellationToken) =>
        OkOrError(await _claimService.UpdateStatusAsync(CurrentUser, id, dto, cancellationToken));

    /// <summary>The owning client deletes a draft claim.</summary>
    [HttpDelete("{id:int}")]
    [Authorize(Roles = Roles.Client)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken) =>
        NoContentOrError(await _claimService.DeleteAsync(CurrentUser, id, cancellationToken));
}
