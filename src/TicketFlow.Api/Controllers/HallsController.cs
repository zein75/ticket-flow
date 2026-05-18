using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TicketFlow.Api.Auth;
using TicketFlow.Api.Contracts.Halls;
using TicketFlow.Api.Routing;
using TicketFlow.Application.Features.Halls;
using TicketFlow.Domain.Enums;

namespace TicketFlow.Api.Controllers;

[ApiController]
[Route(ApiRoutes.V1 + "/halls")]
public sealed class HallsController : ControllerBase
{
    private readonly IHallManagementService _halls;

    public HallsController(IHallManagementService halls)
    {
        _halls = halls;
    }

    [HttpPost]
    [Authorize(Policy = AuthorizationPolicies.OrganizerOrAdmin)]
    [ProducesResponseType<HallResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Create(CreateHallRequest request, CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUser(out var userId, out var role))
        {
            return Unauthorized();
        }

        try
        {
            var result = await _halls.CreateAsync(
                new CreateHallCommand(userId, role, request.Name, request.Rows, request.SeatsPerRow),
                cancellationToken);

            return CreatedAtAction(nameof(GetById), new { id = result.Id }, ToResponse(result));
        }
        catch (HallManagementException exception)
        {
            return ToProblemResult(exception);
        }
    }

    [HttpGet]
    [AllowAnonymous]
    [ProducesResponseType<IReadOnlyCollection<HallResponse>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> List(CancellationToken cancellationToken)
    {
        var result = await _halls.ListAsync(cancellationToken);

        return Ok(result.Select(ToResponse).ToList());
    }

    [HttpGet("{id:guid}")]
    [AllowAnonymous]
    [ProducesResponseType<HallResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            var result = await _halls.GetAsync(new GetHallQuery(id), cancellationToken);

            return Ok(ToResponse(result));
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

    private static HallResponse ToResponse(HallResult result)
    {
        return new HallResponse(
            result.Id,
            result.Name,
            result.Rows,
            result.SeatsPerRow,
            result.Seats.Select(ToResponse).ToList());
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
