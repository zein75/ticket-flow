using TicketFlow.Application.Abstractions.Persistence;
using TicketFlow.Application.Abstractions.QrCodes;
using TicketFlow.Domain.Entities;
using TicketFlow.Domain.Enums;

namespace TicketFlow.Application.Features.Tickets;

public sealed class TicketService : ITicketService
{
    private const string AdminRole = "Admin";
    private const string OrganizerRole = "Organizer";

    private readonly ITicketRepository _tickets;
    private readonly ISessionRepository _sessions;
    private readonly IEventRepository _events;
    private readonly IQrCodeImageGenerator _qrCodes;
    private readonly IQrCodePayloadProtector _payloadProtector;

    public TicketService(
        ITicketRepository tickets,
        ISessionRepository sessions,
        IEventRepository events,
        IQrCodeImageGenerator qrCodes,
        IQrCodePayloadProtector payloadProtector)
    {
        _tickets = tickets;
        _sessions = sessions;
        _events = events;
        _qrCodes = qrCodes;
        _payloadProtector = payloadProtector;
    }

    public async Task<IReadOnlyCollection<TicketResult>> ListMineAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var tickets = await _tickets.GetByUserIdAsync(userId, cancellationToken);

        return tickets.Select(ToResult).ToList();
    }

    public async Task<TicketResult> GetAsync(GetTicketQuery query, CancellationToken cancellationToken = default)
    {
        var ticket = await GetAccessibleTicketAsync(query.TicketId, query.UserId, query.Role, cancellationToken);

        return ToResult(ticket);
    }

    public async Task<TicketQrCodeResult> GetQrCodeAsync(GetTicketQrCodeQuery query, CancellationToken cancellationToken = default)
    {
        var ticket = await GetAccessibleTicketAsync(query.TicketId, query.UserId, query.Role, cancellationToken);

        return new TicketQrCodeResult(ticket.Id, _qrCodes.GeneratePng(ticket.QrCodePayload), "image/png");
    }

    public async Task<TicketValidationResult> ValidateAsync(ValidateTicketCommand command, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(command.Payload))
        {
            throw TicketException.InvalidPayload();
        }

        var payload = _payloadProtector.ValidatePayload(command.Payload);
        if (!payload.IsValid || payload.TicketId is null || payload.OrderId is null)
        {
            throw TicketException.InvalidPayload();
        }

        var ticket = await _tickets.GetByIdWithOrderAsync(payload.TicketId.Value, cancellationToken)
            ?? throw TicketException.NotFound();
        await EnsureCanValidateAsync(ticket, command.UserId, command.Role, cancellationToken);

        var isValid = ticket.OrderId == payload.OrderId.Value && ticket.Status == TicketStatus.Active;

        return new TicketValidationResult(ticket.Id, ticket.OrderId, ticket.Status.ToString(), isValid);
    }

    private async Task<Ticket> GetAccessibleTicketAsync(
        Guid ticketId,
        Guid userId,
        string role,
        CancellationToken cancellationToken)
    {
        var ticket = await _tickets.GetByIdWithOrderAsync(ticketId, cancellationToken)
            ?? throw TicketException.NotFound();

        if (role == AdminRole || ticket.Order.UserId == userId)
        {
            return ticket;
        }

        throw TicketException.AccessDenied();
    }

    private async Task EnsureCanValidateAsync(
        Ticket ticket,
        Guid userId,
        string role,
        CancellationToken cancellationToken)
    {
        if (role == AdminRole)
        {
            return;
        }

        if (role != OrganizerRole)
        {
            throw TicketException.AccessDenied();
        }

        var session = await _sessions.GetByIdAsync(ticket.SessionId, cancellationToken)
            ?? throw TicketException.NotFound();
        var ticketEvent = await _events.GetByIdAsync(session.EventId, cancellationToken)
            ?? throw TicketException.NotFound();

        if (ticketEvent.OrganizerId != userId)
        {
            throw TicketException.AccessDenied();
        }
    }

    private static TicketResult ToResult(Ticket ticket) =>
        new(ticket.Id, ticket.OrderId, ticket.SessionId, ticket.SeatId, ticket.Status.ToString());
}
