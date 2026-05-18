using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TicketFlow.Api.Contracts.Reservations;
using TicketFlow.Api.Routing;
using TicketFlow.Application.Features.Reservations;

namespace TicketFlow.Api.Controllers;

[ApiController]
[Route(ApiRoutes.V1 + "/reservations")]
public sealed class ReservationsController : ControllerBase
{
    private readonly IReservationService _reservations;

    public ReservationsController(IReservationService reservations)
    {
        _reservations = reservations;
    }

    [HttpPost]
    [Authorize]
    [ProducesResponseType<ReservationResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Create(CreateReservationRequest request, CancellationToken cancellationToken)
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(userIdClaim, out var userId))
        {
            return Unauthorized();
        }

        try
        {
            var result = await _reservations.CreateAsync(
                new CreateReservationCommand(userId, request.SessionId, request.SeatIds),
                cancellationToken);

            return CreatedAtAction(nameof(Create), new { id = result.Id }, ToResponse(result));
        }
        catch (ReservationException exception)
        {
            return ToProblemResult(exception);
        }
    }

    private static ReservationResponse ToResponse(ReservationResult result)
    {
        return new ReservationResponse(
            result.Id,
            result.UserId,
            result.SessionId,
            result.ExpiresAtUtc,
            result.Status,
            result.SeatIds);
    }

    private ObjectResult ToProblemResult(ReservationException exception)
    {
        var statusCode = exception.Code switch
        {
            "session_not_found" => StatusCodes.Status404NotFound,
            "seat_not_found" => StatusCodes.Status404NotFound,
            "seat_unavailable" => StatusCodes.Status409Conflict,
            _ => StatusCodes.Status400BadRequest
        };

        return Problem(
            title: exception.Message,
            statusCode: statusCode,
            type: $"https://ticketflow/errors/{exception.Code}");
    }
}
