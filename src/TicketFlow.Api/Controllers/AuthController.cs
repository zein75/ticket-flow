using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TicketFlow.Api.Contracts.Auth;
using TicketFlow.Api.Routing;
using TicketFlow.Application.Features.Auth;

namespace TicketFlow.Api.Controllers;

[ApiController]
[Route(ApiRoutes.V1 + "/auth")]
public sealed class AuthController : ControllerBase
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

    [HttpPost("register")]
    [ProducesResponseType<AuthResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Register(RegisterRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var result = await _authService.RegisterAsync(
                new RegisterCommand(request.Email, request.Password),
                cancellationToken);

            return CreatedAtAction(nameof(Me), ToResponse(result));
        }
        catch (AuthException exception)
        {
            return ToProblemResult(exception);
        }
    }

    [HttpPost("login")]
    [ProducesResponseType<AuthResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login(LoginRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var result = await _authService.LoginAsync(new LoginCommand(request.Email, request.Password), cancellationToken);

            return Ok(ToResponse(result));
        }
        catch (AuthException exception)
        {
            return ToProblemResult(exception);
        }
    }

    [HttpPost("refresh")]
    [ProducesResponseType<AuthResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Refresh(RefreshTokenRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var result = await _authService.RefreshAsync(new RefreshTokenCommand(request.RefreshToken), cancellationToken);

            return Ok(ToResponse(result));
        }
        catch (AuthException exception)
        {
            return ToProblemResult(exception);
        }
    }

    [HttpPost("logout")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Logout(LogoutRequest request, CancellationToken cancellationToken)
    {
        await _authService.LogoutAsync(new LogoutCommand(request.RefreshToken), cancellationToken);

        return NoContent();
    }

    [Authorize]
    [HttpGet("me")]
    [ProducesResponseType<CurrentUserResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public IActionResult Me()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var email = User.FindFirstValue(ClaimTypes.Email);
        var role = User.FindFirstValue(ClaimTypes.Role);

        if (!Guid.TryParse(userId, out var parsedUserId) || email is null || role is null)
        {
            return Unauthorized();
        }

        return Ok(new CurrentUserResponse(parsedUserId, email, role));
    }

    private static AuthResponse ToResponse(AuthResult result)
    {
        return new AuthResponse(
            result.UserId,
            result.Email,
            result.Role.ToString(),
            result.AccessToken,
            result.RefreshToken,
            result.AccessTokenExpiresAtUtc,
            result.RefreshTokenExpiresAtUtc);
    }

    private ObjectResult ToProblemResult(AuthException exception)
    {
        var statusCode = exception.Code switch
        {
            "email_already_registered" => StatusCodes.Status409Conflict,
            "role_not_allowed" => StatusCodes.Status400BadRequest,
            "user_blocked" => StatusCodes.Status403Forbidden,
            _ => StatusCodes.Status401Unauthorized
        };

        return Problem(
            title: exception.Message,
            statusCode: statusCode,
            type: $"https://ticketflow/errors/{exception.Code}");
    }
}
