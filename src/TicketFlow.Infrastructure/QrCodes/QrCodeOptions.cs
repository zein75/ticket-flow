namespace TicketFlow.Infrastructure.QrCodes;

internal sealed class QrCodeOptions
{
    public const string SectionName = "QrCodes";

    public string SigningKey { get; init; } = string.Empty;
}
