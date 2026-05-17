using TicketFlow.Domain.Abstractions;
using TicketFlow.Domain.Enums;

namespace TicketFlow.Domain.Entities;

public sealed class Event : Entity
{
    private readonly List<Session> _sessions = [];

    private Event()
    {
        Title = string.Empty;
        Description = string.Empty;
    }

    public Event(Guid id, string title, string description, Guid organizerId)
        : base(id)
    {
        Title = string.IsNullOrWhiteSpace(title) ? throw new ArgumentException("Title is required.", nameof(title)) : title;
        Description = description;
        OrganizerId = organizerId == Guid.Empty ? throw new ArgumentException("Organizer id cannot be empty.", nameof(organizerId)) : organizerId;
        Status = EventStatus.Draft;
    }

    public string Title { get; private set; }

    public string Description { get; private set; }

    public Guid OrganizerId { get; }

    public EventStatus Status { get; private set; }

    public IReadOnlyCollection<Session> Sessions => _sessions.AsReadOnly();

    public void Publish() => Status = EventStatus.Published;

    public void Cancel() => Status = EventStatus.Cancelled;

    public void Archive() => Status = EventStatus.Archived;

    public void Rename(string title, string description)
    {
        Title = string.IsNullOrWhiteSpace(title) ? throw new ArgumentException("Title is required.", nameof(title)) : title;
        Description = description;
    }

    public void AddSession(Session session)
    {
        ArgumentNullException.ThrowIfNull(session);

        if (session.EventId != Id)
        {
            throw new ArgumentException("Session belongs to another event.", nameof(session));
        }

        _sessions.Add(session);
    }
}
