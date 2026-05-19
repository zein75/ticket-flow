using TicketFlow.Application.Abstractions.Persistence;
using TicketFlow.Domain.Entities;
using TicketFlow.Domain.Enums;

namespace TicketFlow.Application.Features.Admin;

public sealed class AdminService : IAdminService
{
    private readonly IUserRepository _users;
    private readonly IOrderRepository _orders;
    private readonly IEventRepository _events;
    private readonly IUnitOfWork _unitOfWork;

    public AdminService(
        IUserRepository users,
        IOrderRepository orders,
        IEventRepository events,
        IUnitOfWork unitOfWork)
    {
        _users = users;
        _orders = orders;
        _events = events;
        _unitOfWork = unitOfWork;
    }

    public async Task<IReadOnlyCollection<AdminUserResult>> ListUsersAsync(CancellationToken cancellationToken = default)
    {
        var users = await _users.GetAllAsync(cancellationToken);

        return users.Select(ToResult).ToList();
    }

    public async Task<AdminUserResult> BlockUserAsync(
        BlockUserCommand command,
        CancellationToken cancellationToken = default)
    {
        if (command.UserId == command.AdminUserId)
        {
            throw AdminException.CannotBlockSelf();
        }

        var user = await _users.GetByIdAsync(command.UserId, cancellationToken)
            ?? throw AdminException.UserNotFound();

        user.Block();
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return ToResult(user);
    }

    public async Task<IReadOnlyCollection<AdminOrderResult>> ListOrdersAsync(CancellationToken cancellationToken = default)
    {
        var orders = await _orders.GetAllWithTicketsAsync(cancellationToken);

        return orders.Select(ToResult).ToList();
    }

    public async Task<ModeratedEventResult> ApproveEventAsync(
        ModerateEventCommand command,
        CancellationToken cancellationToken = default)
    {
        var ticketEvent = await _events.GetByIdAsync(command.EventId, cancellationToken)
            ?? throw AdminException.EventNotFound();

        ticketEvent.ChangeStatus(EventStatus.Published);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return ToResult(ticketEvent);
    }

    private static AdminUserResult ToResult(User user) =>
        new(user.Id, user.Email, user.Role.ToString(), user.IsBlocked);

    private static AdminOrderResult ToResult(Order order) =>
        new(
            order.Id,
            order.UserId,
            order.SessionId,
            order.ReservationId,
            order.TotalAmount.Amount,
            order.TotalAmount.Currency,
            order.Status.ToString(),
            order.Tickets.Select(ticket => ticket.Id).ToList());

    private static ModeratedEventResult ToResult(Event ticketEvent) =>
        new(
            ticketEvent.Id,
            ticketEvent.Title,
            ticketEvent.Description,
            ticketEvent.OrganizerId,
            ticketEvent.Status.ToString());
}
