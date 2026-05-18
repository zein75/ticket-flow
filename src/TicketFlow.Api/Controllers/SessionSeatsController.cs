using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TicketFlow.Api.Contracts.Halls;
using TicketFlow.Api.Routing;
using TicketFlow.Application.Features.Halls;
using TicketFlow.Domain.Enums;

namespace TicketFlow.Api.Controllers;

[ApiController]
[Route(ApiRoutes.V1 + "/sessions/{sessionId:guid}/seats")]
public sealed class SessionSeatsController : ControllerBase
{
    private readonly IHallManagementService _halls;

    public SessionSeatsController(IHallManagementService halls)
    {
        _halls = halls;
    }

    [HttpGet]
    [AllowAnonymous]
    [ProducesResponseType<IReadOnlyCollection<SeatResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> List(Guid sessionId, CancellationToken cancellationToken)
    {
        var query = TryGetCurrentUser(out var userId, out var role)
            ? new ListSessionSeatsQuery(sessionId, userId, role)
            : new ListSessionSeatsQuery(sessionId, null, null);

        try
        {
            var result = await _halls.ListSessionSeatsAsync(query, cancellationToken);

            return Ok(result.Select(ToResponse).ToList());
        }
        catch (HallManagementException exception)
        {
            return ToProblemResult(exception);
        }
    }

    private bool TryGetCurrentUser(out Guid userId, out UserRole role)
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var roleClaim = User.FindFirstValue(ClaimTypes.Role);

        if (Guid.TryParse(userIdClaim, out userId) && Enum.TryParse(roleClaim, out role))
        {
            return true;
        }

        userId = Guid.Empty;
        role = default;
        return false;
    }

    private static SeatResponse ToResponse(SeatResult result)
    {
        return new SeatResponse(result.Id, result.HallId, result.Row, result.Number);
    }

    private ObjectResult ToProblemResult(HallManagementException exception)
    {
        var statusCode = exception.Code switch
        {
            "hall_not_found" => StatusCodes.Status404NotFound,
            "session_not_found" => StatusCodes.Status404NotFound,
            "role_not_allowed" => StatusCodes.Status403Forbidden,
            _ => StatusCodes.Status400BadRequest
        };

        return Problem(
            title: exception.Message,
            statusCode: statusCode,
            type: $"https://ticketflow/errors/{exception.Code}");
    }
}
