using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TicketFlow.Api.Auth;
using TicketFlow.Api.Contracts.Admin;
using TicketFlow.Api.Routing;
using TicketFlow.Application.Features.Admin;

namespace TicketFlow.Api.Controllers;

[ApiController]
[Route(ApiRoutes.V1 + "/admin")]
[Authorize(Policy = AuthorizationPolicies.AdminOnly)]
public sealed class AdminController : ControllerBase
{
    private readonly IAdminService _admin;

    public AdminController(IAdminService admin)
    {
        _admin = admin;
    }

    [HttpGet("users")]
    [ProducesResponseType<IReadOnlyCollection<AdminUserResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> ListUsers(CancellationToken cancellationToken)
    {
        var users = await _admin.ListUsersAsync(cancellationToken);

        return Ok(users.Select(ToResponse).ToList());
    }

    [HttpPatch("users/{id:guid}/block")]
    [ProducesResponseType<AdminUserResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> BlockUser(Guid id, CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(out var adminUserId))
        {
            return Unauthorized();
        }

        try
        {
            var result = await _admin.BlockUserAsync(new BlockUserCommand(id, adminUserId), cancellationToken);

            return Ok(ToResponse(result));
        }
        catch (AdminException exception)
        {
            return ToProblemResult(exception);
        }
    }

    [HttpGet("orders")]
    [ProducesResponseType<IReadOnlyCollection<AdminOrderResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> ListOrders(CancellationToken cancellationToken)
    {
        var orders = await _admin.ListOrdersAsync(cancellationToken);

        return Ok(orders.Select(ToResponse).ToList());
    }

    [HttpPatch("events/{id:guid}/approve")]
    [ProducesResponseType<AdminEventResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ApproveEvent(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            var result = await _admin.ApproveEventAsync(new ModerateEventCommand(id), cancellationToken);

            return Ok(ToResponse(result));
        }
        catch (AdminException exception)
        {
            return ToProblemResult(exception);
        }
    }

    private bool TryGetCurrentUserId(out Guid userId)
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (Guid.TryParse(userIdClaim, out userId))
        {
            return true;
        }

        userId = Guid.Empty;
        return false;
    }

    private static AdminUserResponse ToResponse(AdminUserResult result) =>
        new(result.Id, result.Email, result.Role, result.IsBlocked);

    private static AdminOrderResponse ToResponse(AdminOrderResult result) =>
        new(
            result.Id,
            result.UserId,
            result.SessionId,
            result.ReservationId,
            result.TotalAmount,
            result.Currency,
            result.Status,
            result.TicketIds);

    private static AdminEventResponse ToResponse(ModeratedEventResult result) =>
        new(result.Id, result.Title, result.Description, result.OrganizerId, result.Status);

    private ObjectResult ToProblemResult(AdminException exception)
    {
        var statusCode = exception.Code switch
        {
            "user_not_found" => StatusCodes.Status404NotFound,
            "event_not_found" => StatusCodes.Status404NotFound,
            "cannot_block_self" => StatusCodes.Status400BadRequest,
            _ => StatusCodes.Status400BadRequest
        };

        return Problem(
            title: exception.Message,
            statusCode: statusCode,
            type: $"https://ticketflow/errors/{exception.Code}");
    }
}
