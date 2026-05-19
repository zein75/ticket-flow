using QRCoder;
using TicketFlow.Application.Abstractions.QrCodes;

namespace TicketFlow.Infrastructure.QrCodes;

internal sealed class QrCodeImageGenerator : IQrCodeImageGenerator
{
    public byte[] GeneratePng(string payload)
    {
        using var generator = new QRCodeGenerator();
        using var data = generator.CreateQrCode(payload, QRCodeGenerator.ECCLevel.Q);
        using var qrCode = new PngByteQRCode(data);

        return qrCode.GetGraphic(12);
    }
}
