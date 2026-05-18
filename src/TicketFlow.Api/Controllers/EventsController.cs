using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TicketFlow.Api.Auth;
using TicketFlow.Api.Contracts.Events;
using TicketFlow.Api.Routing;
using TicketFlow.Application.Features.Events;
using TicketFlow.Domain.Enums;

namespace TicketFlow.Api.Controllers;

[ApiController]
[Route(ApiRoutes.V1 + "/events")]
public sealed class EventsController : ControllerBase
{
    private readonly IEventManagementService _events;

    public EventsController(IEventManagementService events)
    {
        _events = events;
    }

    [HttpPost]
    [Authorize(Policy = AuthorizationPolicies.OrganizerOrAdmin)]
    [ProducesResponseType<EventResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Create(CreateEventRequest request, CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUser(out var userId, out var role))
        {
            return Unauthorized();
        }

        try
        {
            var result = await _events.CreateAsync(
                new CreateEventCommand(userId, role, request.Title, request.Description, request.Status),
                cancellationToken);

            return CreatedAtAction(nameof(GetById), new { id = result.Id }, ToResponse(result));
        }
        catch (EventManagementException exception)
        {
            return ToProblemResult(exception);
        }
    }

    [HttpGet]
    [AllowAnonymous]
    [ProducesResponseType<IReadOnlyCollection<EventResponse>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> List(CancellationToken cancellationToken)
    {
        var query = TryGetCurrentUser(out var userId, out var role)
            ? new ListEventsQuery(userId, role)
            : new ListEventsQuery(null, null);
        var result = await _events.ListAsync(query, cancellationToken);

        return Ok(result.Select(ToResponse).ToList());
    }

    [HttpGet("{id:guid}")]
    [AllowAnonymous]
    [ProducesResponseType<EventResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var query = TryGetCurrentUser(out var userId, out var role)
            ? new GetEventQuery(id, userId, role)
            : new GetEventQuery(id, null, null);

        try
        {
            var result = await _events.GetAsync(query, cancellationToken);

            return Ok(ToResponse(result));
        }
        catch (EventManagementException exception)
        {
            return ToProblemResult(exception);
        }
    }

    [HttpPut("{id:guid}")]
    [Authorize(Policy = AuthorizationPolicies.OrganizerOrAdmin)]
    [ProducesResponseType<EventResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(Guid id, UpdateEventRequest request, CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUser(out var userId, out var role))
        {
            return Unauthorized();
        }

        try
        {
            var result = await _events.UpdateAsync(
                new UpdateEventCommand(id, userId, role, request.Title, request.Description, request.Status),
                cancellationToken);

            return Ok(ToResponse(result));
        }
        catch (EventManagementException exception)
        {
            return ToProblemResult(exception);
        }
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Policy = AuthorizationPolicies.OrganizerOrAdmin)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUser(out var userId, out var role))
        {
            return Unauthorized();
        }

        try
        {
            await _events.DeleteAsync(new DeleteEventCommand(id, userId, role), cancellationToken);

            return NoContent();
        }
        catch (EventManagementException exception)
        {
            return ToProblemResult(exception);
        }
    }

    [HttpPost("{eventId:guid}/sessions")]
    [Authorize(Policy = AuthorizationPolicies.OrganizerOrAdmin)]
    [ProducesResponseType<SessionResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CreateSession(Guid eventId, CreateSessionRequest request, CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUser(out var userId, out var role))
        {
            return Unauthorized();
        }

        try
        {
            var result = await _events.CreateSessionAsync(
                new CreateSessionCommand(
                    eventId,
                    userId,
                    role,
                    request.HallId,
                    request.StartsAtUtc,
                    request.EndsAtUtc,
                    request.PriceAmount,
                    request.PriceCurrency),
                cancellationToken);

            return CreatedAtAction(nameof(ListSessions), new { eventId }, ToResponse(result));
        }
        catch (EventManagementException exception)
        {
            return ToProblemResult(exception);
        }
    }

    [HttpGet("{eventId:guid}/sessions")]
    [AllowAnonymous]
    [ProducesResponseType<IReadOnlyCollection<SessionResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ListSessions(Guid eventId, CancellationToken cancellationToken)
    {
        var query = TryGetCurrentUser(out var userId, out var role)
            ? new ListEventSessionsQuery(eventId, userId, role)
            : new ListEventSessionsQuery(eventId, null, null);

        try
        {
            var result = await _events.ListSessionsAsync(query, cancellationToken);

            return Ok(result.Select(ToResponse).ToList());
        }
        catch (EventManagementException exception)
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

    private static EventResponse ToResponse(EventResult result)
    {
        return new EventResponse(result.Id, result.Title, result.Description, result.OrganizerId, result.Status);
    }

    private static SessionResponse ToResponse(SessionResult result)
    {
        return new SessionResponse(
            result.Id,
            result.EventId,
            result.HallId,
            result.StartsAtUtc,
            result.EndsAtUtc,
            result.PriceAmount,
            result.PriceCurrency);
    }

    private ObjectResult ToProblemResult(EventManagementException exception)
    {
        var statusCode = exception.Code switch
        {
            "event_not_found" => StatusCodes.Status404NotFound,
            "event_access_denied" => StatusCodes.Status403Forbidden,
            "role_not_allowed" => StatusCodes.Status403Forbidden,
            "hall_not_found" => StatusCodes.Status404NotFound,
            _ => StatusCodes.Status400BadRequest
        };

        return Problem(
            title: exception.Message,
            statusCode: statusCode,
            type: $"https://ticketflow/errors/{exception.Code}");
    }
}
