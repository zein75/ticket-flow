using TicketFlow.Application.Abstractions.Persistence;
using TicketFlow.Domain.Entities;
using TicketFlow.Domain.Enums;
using TicketFlow.Domain.ValueObjects;

namespace TicketFlow.Application.Features.Events;

public sealed class EventManagementService : IEventManagementService
{
    private readonly IEventRepository _events;
    private readonly ISessionRepository _sessions;
    private readonly IHallRepository _halls;
    private readonly IUnitOfWork _unitOfWork;

    public EventManagementService(
        IEventRepository events,
        ISessionRepository sessions,
        IHallRepository halls,
        IUnitOfWork unitOfWork)
    {
        _events = events;
        _sessions = sessions;
        _halls = halls;
        _unitOfWork = unitOfWork;
    }

    public async Task<EventResult> CreateAsync(CreateEventCommand command, CancellationToken cancellationToken = default)
    {
        EnsureCanManageEvents(command.Role);

        try
        {
            var ticketEvent = new Event(Guid.NewGuid(), command.Title.Trim(), command.Description.Trim(), command.UserId);
            ticketEvent.ChangeStatus(command.Status);

            _events.Add(ticketEvent);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return ToResult(ticketEvent);
        }
        catch (ArgumentException exception)
        {
            throw EventManagementException.Validation(exception.Message);
        }
    }

    public async Task<IReadOnlyCollection<EventResult>> ListAsync(ListEventsQuery query, CancellationToken cancellationToken = default)
    {
        IReadOnlyCollection<Event> events = query.Role switch
        {
            UserRole.Admin => await _events.GetAllAsync(cancellationToken),
            UserRole.Organizer when query.UserId is not null => await _events.GetByOrganizerIdAsync(query.UserId.Value, cancellationToken),
            _ => await _events.GetPublishedAsync(cancellationToken)
        };

        return events.Select(ToResult).ToList();
    }

    public async Task<EventResult> GetAsync(GetEventQuery query, CancellationToken cancellationToken = default)
    {
        var ticketEvent = await _events.GetByIdAsync(query.EventId, cancellationToken)
            ?? throw EventManagementException.NotFound();

        if (!CanView(ticketEvent, query.UserId, query.Role))
        {
            throw EventManagementException.NotFound();
        }

        return ToResult(ticketEvent);
    }

    public async Task<EventResult> UpdateAsync(UpdateEventCommand command, CancellationToken cancellationToken = default)
    {
        EnsureCanManageEvents(command.Role);

        var ticketEvent = await _events.GetByIdAsync(command.EventId, cancellationToken)
            ?? throw EventManagementException.NotFound();
        EnsureCanEdit(ticketEvent, command.UserId, command.Role);

        try
        {
            ticketEvent.Rename(command.Title.Trim(), command.Description.Trim());
            ticketEvent.ChangeStatus(command.Status);

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return ToResult(ticketEvent);
        }
        catch (ArgumentException exception)
        {
            throw EventManagementException.Validation(exception.Message);
        }
    }

    public async Task DeleteAsync(DeleteEventCommand command, CancellationToken cancellationToken = default)
    {
        EnsureCanManageEvents(command.Role);

        var ticketEvent = await _events.GetByIdAsync(command.EventId, cancellationToken)
            ?? throw EventManagementException.NotFound();
        EnsureCanEdit(ticketEvent, command.UserId, command.Role);

        _events.Remove(ticketEvent);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task<SessionResult> CreateSessionAsync(CreateSessionCommand command, CancellationToken cancellationToken = default)
    {
        EnsureCanManageEvents(command.Role);

        var ticketEvent = await _events.GetByIdAsync(command.EventId, cancellationToken)
            ?? throw EventManagementException.NotFound();
        EnsureCanEdit(ticketEvent, command.UserId, command.Role);

        if (!await _halls.ExistsAsync(command.HallId, cancellationToken))
        {
            throw EventManagementException.HallNotFound();
        }

        try
        {
            var session = new Session(
                Guid.NewGuid(),
                command.EventId,
                command.HallId,
                command.StartsAtUtc,
                command.EndsAtUtc,
                new Money(command.PriceAmount, command.PriceCurrency.Trim()));

            ticketEvent.AddSession(session);
            _sessions.Add(session);

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return ToResult(session);
        }
        catch (ArgumentException exception)
        {
            throw EventManagementException.Validation(exception.Message);
        }
    }

    public async Task<IReadOnlyCollection<SessionResult>> ListSessionsAsync(
        ListEventSessionsQuery query,
        CancellationToken cancellationToken = default)
    {
        var ticketEvent = await _events.GetByIdAsync(query.EventId, cancellationToken)
            ?? throw EventManagementException.NotFound();

        if (!CanView(ticketEvent, query.UserId, query.Role))
        {
            throw EventManagementException.NotFound();
        }

        var sessions = await _sessions.GetByEventIdAsync(query.EventId, cancellationToken);

        return sessions.Select(ToResult).ToList();
    }

    private static void EnsureCanManageEvents(UserRole role)
    {
        if (role is not (UserRole.Organizer or UserRole.Admin))
        {
            throw EventManagementException.RoleNotAllowed();
        }
    }

    private static void EnsureCanEdit(Event ticketEvent, Guid userId, UserRole role)
    {
        if (role == UserRole.Admin)
        {
            return;
        }

        if (ticketEvent.OrganizerId != userId)
        {
            throw EventManagementException.Forbidden();
        }
    }

    private static bool CanView(Event ticketEvent, Guid? userId, UserRole? role)
    {
        if (ticketEvent.Status == EventStatus.Published || role == UserRole.Admin)
        {
            return true;
        }

        return role == UserRole.Organizer && userId == ticketEvent.OrganizerId;
    }

    private static EventResult ToResult(Event ticketEvent)
    {
        return new EventResult(
            ticketEvent.Id,
            ticketEvent.Title,
            ticketEvent.Description,
            ticketEvent.OrganizerId,
            ticketEvent.Status.ToString());
    }

    private static SessionResult ToResult(Session session)
    {
        return new SessionResult(
            session.Id,
            session.EventId,
            session.HallId,
            session.StartsAtUtc,
            session.EndsAtUtc,
            session.Price.Amount,
            session.Price.Currency);
    }
}
