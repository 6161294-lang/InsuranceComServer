using InsuranceClaims.Core.DTOs;
using InsuranceClaims.Core.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace InsuranceClaims.API.Controllers;

[Route("api/auth")]
[AllowAnonymous]
public class AuthController : ApiControllerBase
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

    /// <summary>Registers a new client. The password is stored only as a BCrypt hash.</summary>
    [HttpPost("register")]
    [ProducesResponseType(typeof(ClientDto), StatusCodes.Status201Created)]
    public async Task<ActionResult<ClientDto>> Register(RegisterClientDto dto, CancellationToken cancellationToken)
    {
        var result = await _authService.RegisterClientAsync(dto, cancellationToken);
        if (!result.IsSuccess)
        {
            return Error(result);
        }

        return Created($"/api/clients/{result.Value!.Id}", result.Value);
    }

    /// <summary>Logs in a client, a manager or an appraiser and returns a JWT to send as "Authorization: Bearer {token}".</summary>
    [HttpPost("login")]
    [ProducesResponseType(typeof(AuthResponseDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<AuthResponseDto>> Login(LoginDto dto, CancellationToken cancellationToken) =>
        OkOrError(await _authService.LoginAsync(dto, cancellationToken));
}
