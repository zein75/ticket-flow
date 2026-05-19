using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TicketFlow.Api.Auth;
using TicketFlow.Api.Contracts.Tickets;
using TicketFlow.Api.Routing;
using TicketFlow.Application.Features.Tickets;

namespace TicketFlow.Api.Controllers;

[ApiController]
[Route(ApiRoutes.V1 + "/tickets")]
public sealed class TicketsController : ControllerBase
{
    private readonly ITicketService _tickets;

    public TicketsController(ITicketService tickets)
    {
        _tickets = tickets;
    }

    [HttpGet("my")]
    [Authorize]
    [ProducesResponseType<IReadOnlyCollection<TicketResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> ListMine(CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUser(out var userId, out _))
        {
            return Unauthorized();
        }

        var result = await _tickets.ListMineAsync(userId, cancellationToken);

        return Ok(result.Select(ToResponse).ToList());
    }

    [HttpGet("{id:guid}")]
    [Authorize]
    [ProducesResponseType<TicketResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUser(out var userId, out var role))
        {
            return Unauthorized();
        }

        try
        {
            var result = await _tickets.GetAsync(new GetTicketQuery(id, userId, role), cancellationToken);

            return Ok(ToResponse(result));
        }
        catch (TicketException exception)
        {
            return ToProblemResult(exception);
        }
    }

    [HttpGet("{id:guid}/qr")]
    [Authorize]
    [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetQrCode(Guid id, CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUser(out var userId, out var role))
        {
            return Unauthorized();
        }

        try
        {
            var result = await _tickets.GetQrCodeAsync(new GetTicketQrCodeQuery(id, userId, role), cancellationToken);

            return File(result.Image, result.ContentType);
        }
        catch (TicketException exception)
        {
            return ToProblemResult(exception);
        }
    }

    [HttpPost("validate")]
    [Authorize(Policy = AuthorizationPolicies.OrganizerOrAdmin)]
    [ProducesResponseType<ValidateTicketResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Validate(ValidateTicketRequest request, CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUser(out var userId, out var role))
        {
            return Unauthorized();
        }

        try
        {
            var result = await _tickets.ValidateAsync(new ValidateTicketCommand(request.Payload, userId, role), cancellationToken);

            return Ok(new ValidateTicketResponse(result.TicketId, result.OrderId, result.Status, result.IsValid));
        }
        catch (TicketException exception)
        {
            return ToProblemResult(exception);
        }
    }

    private bool TryGetCurrentUser(out Guid userId, out string role)
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        role = User.FindFirstValue(ClaimTypes.Role) ?? string.Empty;

        if (Guid.TryParse(userIdClaim, out userId) && !string.IsNullOrWhiteSpace(role))
        {
            return true;
        }

        userId = Guid.Empty;
        role = string.Empty;
        return false;
    }

    private static TicketResponse ToResponse(TicketResult result) =>
        new(result.Id, result.OrderId, result.SessionId, result.SeatId, result.Status);

    private ObjectResult ToProblemResult(TicketException exception)
    {
        var statusCode = exception.Code switch
        {
            "ticket_not_found" => StatusCodes.Status404NotFound,
            "ticket_access_denied" => StatusCodes.Status403Forbidden,
            "invalid_ticket_payload" => StatusCodes.Status400BadRequest,
            _ => StatusCodes.Status400BadRequest
        };

        return Problem(
            title: exception.Message,
            statusCode: statusCode,
            type: $"https://ticketflow/errors/{exception.Code}");
    }
}
