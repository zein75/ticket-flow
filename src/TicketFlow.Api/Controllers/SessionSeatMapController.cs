using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TicketFlow.Api.Contracts.SeatMaps;
using TicketFlow.Api.Routing;
using TicketFlow.Application.Features.SeatMaps;
using TicketFlow.Domain.Enums;

namespace TicketFlow.Api.Controllers;

[ApiController]
[Route(ApiRoutes.V1 + "/sessions/{sessionId:guid}/seat-map")]
public sealed class SessionSeatMapController : ControllerBase
{
    private readonly ISeatMapService _seatMaps;

    public SessionSeatMapController(ISeatMapService seatMaps)
    {
        _seatMaps = seatMaps;
    }

    [HttpGet]
    [AllowAnonymous]
    [ProducesResponseType<SeatMapResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Get(Guid sessionId, CancellationToken cancellationToken)
    {
        var query = TryGetCurrentUser(out var userId, out var role)
            ? new GetSessionSeatMapQuery(sessionId, userId, role)
            : new GetSessionSeatMapQuery(sessionId, null, null);

        try
        {
            var result = await _seatMaps.GetAsync(query, cancellationToken);

            return Ok(ToResponse(result));
        }
        catch (SeatMapException exception)
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

    private static SeatMapResponse ToResponse(SeatMapResult result)
    {
        return new SeatMapResponse(
            result.SessionId,
            result.Seats
                .Select(seat => new SeatMapItemResponse(seat.SeatId, seat.Row, seat.Number, seat.Status.ToString()))
                .ToList());
    }

    private ObjectResult ToProblemResult(SeatMapException exception)
    {
        var statusCode = exception.Code switch
        {
            "session_not_found" => StatusCodes.Status404NotFound,
            _ => StatusCodes.Status400BadRequest
        };

        return Problem(
            title: exception.Message,
            statusCode: statusCode,
            type: $"https://ticketflow/errors/{exception.Code}");
    }
}
