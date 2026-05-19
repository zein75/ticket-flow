using TicketFlow.Application.Abstractions.Common;
using TicketFlow.Application.Abstractions.Persistence;
using TicketFlow.Domain.Entities;
using TicketFlow.Domain.Enums;

namespace TicketFlow.Application.Features.OrganizerDashboard;

public sealed class OrganizerDashboardService : IOrganizerDashboardService
{
    private const string DefaultCurrency = "USD";

    private readonly IEventRepository _events;
    private readonly ISessionRepository _sessions;
    private readonly IOrderRepository _orders;
    private readonly IReservationRepository _reservations;
    private readonly IClock _clock;

    public OrganizerDashboardService(
        IEventRepository events,
        ISessionRepository sessions,
        IOrderRepository orders,
        IReservationRepository reservations,
        IClock clock)
    {
        _events = events;
        _sessions = sessions;
        _orders = orders;
        _reservations = reservations;
        _clock = clock;
    }

    public async Task<OrganizerDashboardResult> GetDashboardAsync(
        GetOrganizerDashboardQuery query,
        CancellationToken cancellationToken = default)
    {
        var events = query.Role == UserRole.Admin
            ? await _events.GetAllAsync(cancellationToken)
            : await _events.GetByOrganizerIdAsync(query.UserId, cancellationToken);

        return await BuildResultAsync(events, cancellationToken);
    }

    public async Task<OrganizerDashboardResult> GetEventStatsAsync(
        GetOrganizerEventStatsQuery query,
        CancellationToken cancellationToken = default)
    {
        var ticketEvent = await _events.GetByIdAsync(query.EventId, cancellationToken)
            ?? throw OrganizerDashboardException.EventNotFound();

        if (query.Role != UserRole.Admin && ticketEvent.OrganizerId != query.UserId)
        {
            throw OrganizerDashboardException.AccessDenied();
        }

        return await BuildResultAsync([ticketEvent], cancellationToken);
    }

    private async Task<OrganizerDashboardResult> BuildResultAsync(
        IReadOnlyCollection<Event> events,
        CancellationToken cancellationToken)
    {
        if (events.Count == 0)
        {
            return EmptyResult();
        }

        var eventIds = events.Select(ticketEvent => ticketEvent.Id).ToArray();
        var sessions = await _sessions.GetByEventIdsAsync(eventIds, cancellationToken);
        if (sessions.Count == 0)
        {
            return EmptyResult();
        }

        var sessionIds = sessions.Select(session => session.Id).ToArray();
        var paidOrders = await _orders.GetPaidBySessionIdsWithTicketsAsync(sessionIds, cancellationToken);
        var reservations = await _reservations.GetBySessionIdsWithSeatsAsync(sessionIds, cancellationToken);
        var activeReservations = reservations
            .Where(reservation => reservation.Status == ReservationStatus.Active && !reservation.IsExpired(_clock.UtcNow))
            .ToArray();
        var salesBySession = BuildSalesBySession(sessions, paidOrders);

        var currency = ResolveCurrency(paidOrders, sessions);
        var totalRevenue = paidOrders.Sum(order => order.TotalAmount.Amount);
        var ticketsSold = paidOrders.Sum(CountActiveTickets);
        var seatsReserved = activeReservations.Sum(reservation => reservation.Seats.Count);
        var conversionRate = reservations.Count == 0
            ? 0m
            : Math.Round((decimal)paidOrders.Count / reservations.Count, 4, MidpointRounding.AwayFromZero);

        return new OrganizerDashboardResult(
            totalRevenue,
            currency,
            ticketsSold,
            seatsReserved,
            conversionRate,
            salesBySession);
    }

    private static IReadOnlyCollection<SalesBySessionResult> BuildSalesBySession(
        IReadOnlyCollection<Session> sessions,
        IReadOnlyCollection<Order> paidOrders)
    {
        var ordersBySession = paidOrders
            .GroupBy(order => order.SessionId)
            .ToDictionary(group => group.Key, group => group.ToArray());

        return sessions
            .OrderBy(session => session.StartsAtUtc)
            .Select(session =>
            {
                ordersBySession.TryGetValue(session.Id, out var sessionOrders);
                sessionOrders ??= [];

                return new SalesBySessionResult(
                    session.Id,
                    session.EventId,
                    session.StartsAtUtc,
                    sessionOrders.Sum(order => order.TotalAmount.Amount),
                    session.Price.Currency,
                    sessionOrders.Sum(CountActiveTickets));
            })
            .ToList();
    }

    private static int CountActiveTickets(Order order) =>
        order.Tickets.Count(ticket => ticket.Status == TicketStatus.Active);

    private static string ResolveCurrency(IReadOnlyCollection<Order> paidOrders, IReadOnlyCollection<Session> sessions)
    {
        if (paidOrders.Count > 0)
        {
            return paidOrders.Select(order => order.TotalAmount.Currency).Distinct().SingleOrDefault() ?? "MIXED";
        }

        return sessions.Select(session => session.Price.Currency).Distinct().SingleOrDefault() ?? DefaultCurrency;
    }

    private static OrganizerDashboardResult EmptyResult() =>
        new(0m, DefaultCurrency, 0, 0, 0m, []);
}
