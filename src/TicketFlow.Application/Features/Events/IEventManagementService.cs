namespace TicketFlow.Application.Features.Events;

public interface IEventManagementService
{
    Task<EventResult> CreateAsync(CreateEventCommand command, CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<EventResult>> ListAsync(ListEventsQuery query, CancellationToken cancellationToken = default);

    Task<EventResult> GetAsync(GetEventQuery query, CancellationToken cancellationToken = default);

    Task<EventResult> UpdateAsync(UpdateEventCommand command, CancellationToken cancellationToken = default);

    Task DeleteAsync(DeleteEventCommand command, CancellationToken cancellationToken = default);

    Task<SessionResult> CreateSessionAsync(CreateSessionCommand command, CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<SessionResult>> ListSessionsAsync(ListEventSessionsQuery query, CancellationToken cancellationToken = default);
}
