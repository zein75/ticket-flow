using TicketFlow.Application.Abstractions.Common;
using TicketFlow.Application.Abstractions.Payments;
using TicketFlow.Application.Abstractions.Persistence;
using TicketFlow.Application.Abstractions.QrCodes;
using TicketFlow.Application.Abstractions.Realtime;
using TicketFlow.Application.Abstractions.Reservations;
using TicketFlow.Domain.Entities;
using TicketFlow.Domain.Enums;

namespace TicketFlow.Application.Features.Payments;

public sealed class PaymentService : IPaymentService
{
    private readonly IReservationRepository _reservations;
    private readonly ISessionRepository _sessions;
    private readonly IOrderRepository _orders;
    private readonly IPaymentRepository _payments;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ISeatLockStore _seatLocks;
    private readonly ISeatStatusNotifier _seatStatusNotifier;
    private readonly IPaymentProvider _paymentProvider;
    private readonly IQrCodePayloadProtector _qrCodePayloads;
    private readonly IClock _clock;

    public PaymentService(
        IReservationRepository reservations,
        ISessionRepository sessions,
        IOrderRepository orders,
        IPaymentRepository payments,
        IUnitOfWork unitOfWork,
        ISeatLockStore seatLocks,
        ISeatStatusNotifier seatStatusNotifier,
        IPaymentProvider paymentProvider,
        IQrCodePayloadProtector qrCodePayloads,
        IClock clock)
    {
        _reservations = reservations;
        _sessions = sessions;
        _orders = orders;
        _payments = payments;
        _unitOfWork = unitOfWork;
        _seatLocks = seatLocks;
        _seatStatusNotifier = seatStatusNotifier;
        _paymentProvider = paymentProvider;
        _qrCodePayloads = qrCodePayloads;
        _clock = clock;
    }

    public async Task<PaymentResult> PayReservationAsync(PayReservationCommand command, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(command.CardNumber))
        {
            throw PaymentException.Validation("Card number is required.");
        }

        var reservation = await _reservations.GetByIdWithSeatsAsync(command.ReservationId, cancellationToken)
            ?? throw PaymentException.ReservationNotFound();

        if (reservation.UserId != command.UserId)
        {
            throw PaymentException.ReservationAccessDenied();
        }

        if (reservation.Status != ReservationStatus.Active || reservation.IsExpired(_clock.UtcNow))
        {
            throw PaymentException.ReservationUnavailable();
        }

        var session = await _sessions.GetByIdAsync(reservation.SessionId, cancellationToken)
            ?? throw PaymentException.ReservationUnavailable();
        var totalAmount = session.Price * reservation.Seats.Count;
        var paymentResult = await _paymentProvider.PayAsync(
            new PaymentProviderRequest(
                reservation.Id,
                reservation.UserId,
                totalAmount.Amount,
                session.Price.Currency,
                command.CardNumber),
            cancellationToken);

        if (!paymentResult.Succeeded)
        {
            return new PaymentResult(
                reservation.Id,
                null,
                false,
                "Failed",
                null,
                paymentResult.FailureReason,
                []);
        }

        var orderId = Guid.NewGuid();
        var ticketIds = Array.Empty<Guid>();
        var seatIds = reservation.Seats.Select(seat => seat.SeatId).ToArray();

        try
        {
            await _unitOfWork.ExecuteInTransactionAsync(
                async transactionCancellationToken =>
                {
                    await _reservations.ReloadAsync(reservation, transactionCancellationToken);
                    if (reservation.Status != ReservationStatus.Active || reservation.IsExpired(_clock.UtcNow))
                    {
                        throw PaymentException.ReservationUnavailable();
                    }

                    var order = new Order(orderId, reservation.UserId, reservation.SessionId, reservation.Id, totalAmount);
                    order.MarkPaid();

                    var payment = new Payment(Guid.NewGuid(), order.Id, totalAmount);
                    payment.MarkSucceeded(paymentResult.ProviderPaymentId!);

                    var createdTicketIds = new List<Guid>(reservation.Seats.Count);
                    foreach (var seatId in seatIds)
                    {
                        var ticketId = Guid.NewGuid();
                        var ticket = new Ticket(
                            ticketId,
                            order.Id,
                            reservation.SessionId,
                            seatId,
                            _qrCodePayloads.CreatePayload(ticketId, order.Id));

                        order.AddTicket(ticket);
                        createdTicketIds.Add(ticketId);
                    }

                    reservation.ConvertToOrder(_clock.UtcNow);
                    _orders.Add(order);
                    _payments.Add(payment);

                    await _unitOfWork.SaveChangesAsync(transactionCancellationToken);
                    ticketIds = createdTicketIds.ToArray();
                },
                cancellationToken);
        }
        catch (PersistenceConflictException)
        {
            throw PaymentException.SeatsUnavailable();
        }

        await _seatLocks.ReleaseSeatsAsync(reservation.SessionId, reservation.Id, seatIds, cancellationToken);
        await _seatStatusNotifier.SeatsSoldAsync(reservation.SessionId, orderId, seatIds, cancellationToken);

        return new PaymentResult(
            reservation.Id,
            orderId,
            true,
            "Succeeded",
            paymentResult.ProviderPaymentId,
            null,
            ticketIds);
    }

}
