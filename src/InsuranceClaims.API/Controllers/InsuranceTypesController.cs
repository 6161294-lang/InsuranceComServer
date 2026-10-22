using InsuranceClaims.API.Security;
using InsuranceClaims.Core.DTOs;
using InsuranceClaims.Core.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace InsuranceClaims.API.Controllers;

[Route("api/insurance-types")]
[Authorize]
public class InsuranceTypesController : ApiControllerBase
{
    private readonly IInsuranceTypeService _insuranceTypeService;

    public InsuranceTypesController(IInsuranceTypeService insuranceTypeService)
    {
        _insuranceTypeService = insuranceTypeService;
    }

    /// <summary>All insurance types. Public, so the registration form can list them.</summary>
    [HttpGet]
    [AllowAnonymous]
    [ProducesResponseType(typeof(IReadOnlyList<InsuranceTypeDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<InsuranceTypeDto>>> GetAll(CancellationToken cancellationToken) =>
        Ok(await _insuranceTypeService.GetAllAsync(cancellationToken));

    [HttpGet("{id:int}")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(InsuranceTypeDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<InsuranceTypeDto>> GetById(int id, CancellationToken cancellationToken) =>
        OkOrError(await _insuranceTypeService.GetByIdAsync(id, cancellationToken));

    /// <summary>Creates an insurance type (manager only).</summary>
    [HttpPost]
    [Authorize(Roles = Roles.Manager)]
    [ProducesResponseType(typeof(InsuranceTypeDto), StatusCodes.Status201Created)]
    public async Task<ActionResult<InsuranceTypeDto>> Create(InsuranceTypeCreateDto dto, CancellationToken cancellationToken)
    {
        var result = await _insuranceTypeService.CreateAsync(CurrentUser, dto, cancellationToken);
        return result.IsSuccess
            ? CreatedAtAction(nameof(GetById), new { id = result.Value!.Id }, result.Value)
            : Error(result);
    }

    /// <summary>Updates an insurance type (manager only).</summary>
    [HttpPut("{id:int}")]
    [Authorize(Roles = Roles.Manager)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Update(int id, InsuranceTypeUpdateDto dto, CancellationToken cancellationToken) =>
        NoContentOrError(await _insuranceTypeService.UpdateAsync(CurrentUser, id, dto, cancellationToken));

    /// <summary>Deletes an insurance type that nothing refers to (manager only).</summary>
    [HttpDelete("{id:int}")]
    [Authorize(Roles = Roles.Manager)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken) =>
        NoContentOrError(await _insuranceTypeService.DeleteAsync(CurrentUser, id, cancellationToken));
}
