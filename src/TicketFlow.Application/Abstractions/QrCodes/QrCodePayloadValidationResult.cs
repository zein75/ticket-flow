namespace TicketFlow.Application.Abstractions.QrCodes;

public sealed record QrCodePayloadValidationResult(
    bool IsValid,
    Guid? TicketId,
    Guid? OrderId);
