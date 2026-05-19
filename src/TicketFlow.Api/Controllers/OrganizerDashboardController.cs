using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TicketFlow.Api.Auth;
using TicketFlow.Api.Contracts.Organizer;
using TicketFlow.Api.Routing;
using TicketFlow.Application.Features.OrganizerDashboard;
using TicketFlow.Domain.Enums;

namespace TicketFlow.Api.Controllers;

[ApiController]
[Route(ApiRoutes.V1 + "/organizer")]
[Authorize(Policy = AuthorizationPolicies.OrganizerOrAdmin)]
public sealed class OrganizerDashboardController : ControllerBase
{
    private readonly IOrganizerDashboardService _dashboard;

    public OrganizerDashboardController(IOrganizerDashboardService dashboard)
    {
        _dashboard = dashboard;
    }

    [HttpGet("dashboard")]
    [ProducesResponseType<OrganizerDashboardResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetDashboard(CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUser(out var userId, out var role))
        {
            return Unauthorized();
        }

        var result = await _dashboard.GetDashboardAsync(new GetOrganizerDashboardQuery(userId, role), cancellationToken);

        return Ok(ToResponse(result));
    }

    [HttpGet("events/{eventId:guid}/stats")]
    [ProducesResponseType<OrganizerDashboardResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetEventStats(Guid eventId, CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUser(out var userId, out var role))
        {
            return Unauthorized();
        }

        try
        {
            var result = await _dashboard.GetEventStatsAsync(
                new GetOrganizerEventStatsQuery(eventId, userId, role),
                cancellationToken);

            return Ok(ToResponse(result));
        }
        catch (OrganizerDashboardException exception)
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

    private static OrganizerDashboardResponse ToResponse(OrganizerDashboardResult result)
    {
        return new OrganizerDashboardResponse(
            result.TotalRevenue,
            result.Currency,
            result.TicketsSold,
            result.SeatsReserved,
            result.ConversionRate,
            result.SalesBySession.Select(ToResponse).ToList());
    }

    private static SalesBySessionResponse ToResponse(SalesBySessionResult result)
    {
        return new SalesBySessionResponse(
            result.SessionId,
            result.EventId,
            result.StartsAtUtc,
            result.Revenue,
            result.Currency,
            result.TicketsSold);
    }

    private ObjectResult ToProblemResult(OrganizerDashboardException exception)
    {
        var statusCode = exception.Code switch
        {
            "event_not_found" => StatusCodes.Status404NotFound,
            "event_access_denied" => StatusCodes.Status403Forbidden,
            _ => StatusCodes.Status400BadRequest
        };

        return Problem(
            title: exception.Message,
            statusCode: statusCode,
            type: $"https://ticketflow/errors/{exception.Code}");
    }
}
