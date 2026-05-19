namespace TicketFlow.Application.Abstractions.QrCodes;

public interface IQrCodeImageGenerator
{
    byte[] GeneratePng(string payload);
}
