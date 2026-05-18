using TicketFlow.Application.Abstractions.Persistence;
using TicketFlow.Domain.Entities;
using TicketFlow.Domain.Enums;

namespace TicketFlow.Application.Features.Halls;

public sealed class HallManagementService : IHallManagementService
{
    private readonly IHallRepository _halls;
    private readonly ISessionRepository _sessions;
    private readonly IEventRepository _events;
    private readonly IUnitOfWork _unitOfWork;

    public HallManagementService(
        IHallRepository halls,
        ISessionRepository sessions,
        IEventRepository events,
        IUnitOfWork unitOfWork)
    {
        _halls = halls;
        _sessions = sessions;
        _events = events;
        _unitOfWork = unitOfWork;
    }

    public async Task<HallResult> CreateAsync(CreateHallCommand command, CancellationToken cancellationToken = default)
    {
        EnsureCanManageHalls(command.Role);

        try
        {
            var hall = new Hall(Guid.NewGuid(), command.Name.Trim(), command.Rows, command.SeatsPerRow);

            _halls.Add(hall);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return ToResult(hall);
        }
        catch (ArgumentException exception)
        {
            throw HallManagementException.Validation(exception.Message);
        }
    }

    public async Task<IReadOnlyCollection<HallResult>> ListAsync(CancellationToken cancellationToken = default)
    {
        var halls = await _halls.GetAllAsync(cancellationToken);

        return halls.Select(ToResult).ToList();
    }

    public async Task<HallResult> GetAsync(GetHallQuery query, CancellationToken cancellationToken = default)
    {
        var hall = await _halls.GetByIdWithSeatsAsync(query.HallId, cancellationToken)
            ?? throw HallManagementException.NotFound();

        return ToResult(hall);
    }

    public async Task<IReadOnlyCollection<SeatResult>> ListSessionSeatsAsync(
        ListSessionSeatsQuery query,
        CancellationToken cancellationToken = default)
    {
        var session = await _sessions.GetByIdAsync(query.SessionId, cancellationToken)
            ?? throw HallManagementException.SessionNotFound();
        var ticketEvent = await _events.GetByIdAsync(session.EventId, cancellationToken)
            ?? throw HallManagementException.SessionNotFound();

        if (!CanView(ticketEvent, query.UserId, query.Role))
        {
            throw HallManagementException.SessionNotFound();
        }

        var hall = await _halls.GetByIdWithSeatsAsync(session.HallId, cancellationToken)
            ?? throw HallManagementException.NotFound();

        return hall.Seats
            .OrderBy(seat => seat.Row)
            .ThenBy(seat => seat.Number)
            .Select(ToResult)
            .ToList();
    }

    private static void EnsureCanManageHalls(UserRole role)
    {
        if (role is not (UserRole.Organizer or UserRole.Admin))
        {
            throw HallManagementException.RoleNotAllowed();
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

    private static HallResult ToResult(Hall hall)
    {
        return new HallResult(
            hall.Id,
            hall.Name,
            hall.Rows,
            hall.SeatsPerRow,
            hall.Seats
                .OrderBy(seat => seat.Row)
                .ThenBy(seat => seat.Number)
                .Select(ToResult)
                .ToList());
    }

    private static SeatResult ToResult(Seat seat)
    {
        return new SeatResult(seat.Id, seat.HallId, seat.Row, seat.Number);
    }
}
