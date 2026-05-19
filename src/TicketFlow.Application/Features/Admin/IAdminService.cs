namespace TicketFlow.Application.Features.Admin;

public interface IAdminService
{
    Task<IReadOnlyCollection<AdminUserResult>> ListUsersAsync(CancellationToken cancellationToken = default);

    Task<AdminUserResult> BlockUserAsync(BlockUserCommand command, CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<AdminOrderResult>> ListOrdersAsync(CancellationToken cancellationToken = default);

    Task<ModeratedEventResult> ApproveEventAsync(ModerateEventCommand command, CancellationToken cancellationToken = default);
}
