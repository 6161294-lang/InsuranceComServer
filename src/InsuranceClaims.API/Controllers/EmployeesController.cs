using InsuranceClaims.API.Security;
using InsuranceClaims.Core.DTOs;
using InsuranceClaims.Core.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace InsuranceClaims.API.Controllers;

[Route("api/employees")]
[Authorize(Roles = Roles.Manager)]
public class EmployeesController : ApiControllerBase
{
    private readonly IEmployeeService _employeeService;

    public EmployeesController(IEmployeeService employeeService)
    {
        _employeeService = employeeService;
    }

    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<EmployeeDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<EmployeeDto>>> GetPaged([FromQuery] PagingParameters paging, CancellationToken cancellationToken) =>
        OkOrError(await _employeeService.GetPagedAsync(CurrentUser, paging, cancellationToken));

    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(EmployeeDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<EmployeeDto>> GetById(int id, CancellationToken cancellationToken) =>
        OkOrError(await _employeeService.GetByIdAsync(CurrentUser, id, cancellationToken));

    /// <summary>Creates a manager or an appraiser. The password is stored only as a BCrypt hash.</summary>
    [HttpPost]
    [ProducesResponseType(typeof(EmployeeDto), StatusCodes.Status201Created)]
    public async Task<ActionResult<EmployeeDto>> Create(EmployeeCreateDto dto, CancellationToken cancellationToken)
    {
        var result = await _employeeService.CreateAsync(CurrentUser, dto, cancellationToken);
        return result.IsSuccess
            ? CreatedAtAction(nameof(GetById), new { id = result.Value!.Id }, result.Value)
            : Error(result);
    }

    /// <summary>Deletes an employee who has no claims or referrals. A manager cannot delete themself.</summary>
    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken) =>
        NoContentOrError(await _employeeService.DeleteAsync(CurrentUser, id, cancellationToken));
}
