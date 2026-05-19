namespace TicketFlow.Application.Features.Tickets;

public interface ITicketService
{
    Task<IReadOnlyCollection<TicketResult>> ListMineAsync(Guid userId, CancellationToken cancellationToken = default);

    Task<TicketResult> GetAsync(GetTicketQuery query, CancellationToken cancellationToken = default);

    Task<TicketQrCodeResult> GetQrCodeAsync(GetTicketQrCodeQuery query, CancellationToken cancellationToken = default);

    Task<TicketValidationResult> ValidateAsync(ValidateTicketCommand command, CancellationToken cancellationToken = default);
}
