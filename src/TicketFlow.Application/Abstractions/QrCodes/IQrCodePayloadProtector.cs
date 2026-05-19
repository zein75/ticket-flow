namespace TicketFlow.Application.Abstractions.QrCodes;

public interface IQrCodePayloadProtector
{
    string CreatePayload(Guid ticketId, Guid orderId);

    QrCodePayloadValidationResult ValidatePayload(string payload);
}
