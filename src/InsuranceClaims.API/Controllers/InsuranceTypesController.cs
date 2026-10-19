using InsuranceClaims.API.Models;
using InsuranceClaims.API.Security;
using InsuranceClaims.Core.DTOs;
using InsuranceClaims.Core.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace InsuranceClaims.API.Controllers;

[Route("api/insurance-types")]
[Authorize(Roles = Roles.Manager)]
public class InsuranceTypesController : ApiControllerBase
{
    private readonly IInsuranceTypeService _insuranceTypeService;

    public InsuranceTypesController(IInsuranceTypeService insuranceTypeService)
    {
        _insuranceTypeService = insuranceTypeService;
    }

    /// <summary>All insurance types. Public, because the registration form lists them as specializations.</summary>
    [HttpGet]
    [AllowAnonymous]
    [ProducesResponseType(typeof(IReadOnlyList<InsuranceTypeDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<InsuranceTypeDto>>> GetAll(CancellationToken cancellationToken) =>
        Ok(await _insuranceTypeService.GetAllAsync(cancellationToken));

    [HttpGet("{id:int}")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(InsuranceTypeDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<InsuranceTypeDto>> GetById(int id, CancellationToken cancellationToken) =>
        OkOrError(await _insuranceTypeService.GetByIdAsync(id, cancellationToken));

    /// <summary>Creates an insurance type (manager only).</summary>
    [HttpPost]
    [ProducesResponseType(typeof(InsuranceTypeDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<InsuranceTypeDto>> Create(InsuranceTypeCreateDto dto, CancellationToken cancellationToken)
    {
        var result = await _insuranceTypeService.CreateAsync(dto, cancellationToken);
        return result.IsSuccess
            ? CreatedAtAction(nameof(GetById), new { id = result.Value!.Id }, result.Value)
            : Error(result);
    }

    /// <summary>Updates an insurance type (manager only).</summary>
    [HttpPut("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Update(int id, InsuranceTypeUpdateDto dto, CancellationToken cancellationToken) =>
        NoContentOrError(await _insuranceTypeService.UpdateAsync(id, dto, cancellationToken));

    /// <summary>Deletes an insurance type that has no policies (manager only).</summary>
    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken) =>
        NoContentOrError(await _insuranceTypeService.DeleteAsync(id, cancellationToken));
}
