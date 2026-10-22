using InsuranceClaims.API.Security;
using InsuranceClaims.Core.DTOs;
using InsuranceClaims.Core.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace InsuranceClaims.API.Controllers;

[Route("api/referrals")]
[Authorize(Roles = Roles.Appraiser)]
public class ReferralsController : ApiControllerBase
{
    private readonly IReferralService _referralService;

    public ReferralsController(IReferralService referralService)
    {
        _referralService = referralService;
    }

    /// <summary>The referrals assigned to the authenticated appraiser.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<ReferralDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<ReferralDto>>> GetPaged([FromQuery] PagingParameters paging, CancellationToken cancellationToken) =>
        OkOrError(await _referralService.GetPagedAsync(CurrentUser, paging, cancellationToken));

    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(ReferralDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<ReferralDto>> GetById(int id, CancellationToken cancellationToken) =>
        OkOrError(await _referralService.GetByIdAsync(CurrentUser, id, cancellationToken));

    /// <summary>Updates the assessment notes of a referral that belongs to the appraiser.</summary>
    [HttpPut("{id:int}")]
    [ProducesResponseType(typeof(ReferralDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<ReferralDto>> Update(int id, ReferralUpdateDto dto, CancellationToken cancellationToken) =>
        OkOrError(await _referralService.UpdateAsync(CurrentUser, id, dto, cancellationToken));
}
