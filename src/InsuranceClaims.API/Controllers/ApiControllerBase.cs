using InsuranceClaims.API.Models;
using InsuranceClaims.API.Security;
using InsuranceClaims.Core.Common;
using Microsoft.AspNetCore.Mvc;

namespace InsuranceClaims.API.Controllers;

/// <summary>Translates business results (OperationResult) to HTTP responses with the uniform error body.</summary>
[ApiController]
[Produces("application/json")]
public abstract class ApiControllerBase : ControllerBase
{
    protected int CurrentEmployeeId => User.GetEmployeeId();

    /// <summary>Success without a body becomes 204 No Content.</summary>
    protected IActionResult NoContentOrError(OperationResult result) =>
        result.IsSuccess ? NoContent() : Error(result);

    /// <summary>Success with a body becomes 200 OK.</summary>
    protected ActionResult<T> OkOrError<T>(OperationResult<T> result) =>
        result.IsSuccess ? Ok(result.Value) : Error(result);

    protected ObjectResult Error(OperationResult result)
    {
        var statusCode = result.Status switch
        {
            ResultStatus.NotFound => StatusCodes.Status404NotFound,
            ResultStatus.Conflict => StatusCodes.Status409Conflict,
            ResultStatus.Unauthorized => StatusCodes.Status401Unauthorized,
            ResultStatus.Forbidden => StatusCodes.Status403Forbidden,
            ResultStatus.ValidationFailed => StatusCodes.Status400BadRequest,
            _ => StatusCodes.Status500InternalServerError
        };

        return StatusCode(statusCode, new ErrorResponse
        {
            Status = statusCode,
            Message = result.Error ?? string.Empty,
            CorrelationId = HttpContext.TraceIdentifier
        });
    }
}
