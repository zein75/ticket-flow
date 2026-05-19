using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TicketFlow.Api.Contracts.Payments;
using TicketFlow.Api.Routing;
using TicketFlow.Application.Features.Payments;

namespace TicketFlow.Api.Controllers;

[ApiController]
[Route(ApiRoutes.V1 + "/reservations/{reservationId:guid}/pay")]
public sealed class ReservationPaymentsController : ControllerBase
{
    private readonly IPaymentService _payments;

    public ReservationPaymentsController(IPaymentService payments)
    {
        _payments = payments;
    }

    [HttpPost]
    [Authorize]
    [ProducesResponseType<PaymentResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Pay(Guid reservationId, PayReservationRequest request, CancellationToken cancellationToken)
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(userIdClaim, out var userId))
        {
            return Unauthorized();
        }

        try
        {
            var result = await _payments.PayReservationAsync(
                new PayReservationCommand(reservationId, userId, request.CardNumber),
                cancellationToken);

            return Ok(ToResponse(result));
        }
        catch (PaymentException exception)
        {
            return ToProblemResult(exception);
        }
    }

    private static PaymentResponse ToResponse(PaymentResult result)
    {
        return new PaymentResponse(
            result.ReservationId,
            result.OrderId,
            result.Succeeded,
            result.Status,
            result.ProviderPaymentId,
            result.FailureReason,
            result.TicketIds);
    }

    private ObjectResult ToProblemResult(PaymentException exception)
    {
        var statusCode = exception.Code switch
        {
            "reservation_not_found" => StatusCodes.Status404NotFound,
            "reservation_access_denied" => StatusCodes.Status403Forbidden,
            "reservation_unavailable" => StatusCodes.Status409Conflict,
            "seats_unavailable" => StatusCodes.Status409Conflict,
            _ => StatusCodes.Status400BadRequest
        };

        return Problem(
            title: exception.Message,
            statusCode: statusCode,
            type: $"https://ticketflow/errors/{exception.Code}");
    }
}
