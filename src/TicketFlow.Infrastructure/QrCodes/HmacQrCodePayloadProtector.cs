using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using TicketFlow.Application.Abstractions.QrCodes;

namespace TicketFlow.Infrastructure.QrCodes;

internal sealed class HmacQrCodePayloadProtector : IQrCodePayloadProtector
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private readonly byte[] _key;

    public HmacQrCodePayloadProtector(IOptions<QrCodeOptions> options)
    {
        var signingKey = options.Value.SigningKey;
        if (string.IsNullOrWhiteSpace(signingKey) || signingKey.Length < 32)
        {
            throw new InvalidOperationException("QR code signing key must contain at least 32 characters.");
        }

        _key = Encoding.UTF8.GetBytes(signingKey);
    }

    public string CreatePayload(Guid ticketId, Guid orderId)
    {
        var signature = Sign(ticketId, orderId);

        return JsonSerializer.Serialize(
            new TicketQrPayload(
                ticketId.ToString("D"),
                orderId.ToString("D"),
                signature),
            JsonOptions);
    }

    public QrCodePayloadValidationResult ValidatePayload(string payload)
    {
        TicketQrPayload? qrPayload;
        try
        {
            qrPayload = JsonSerializer.Deserialize<TicketQrPayload>(payload, JsonOptions);
        }
        catch (JsonException)
        {
            return new QrCodePayloadValidationResult(false, null, null);
        }

        if (qrPayload is null
            || !Guid.TryParse(qrPayload.TicketId, out var ticketId)
            || !Guid.TryParse(qrPayload.OrderId, out var orderId)
            || string.IsNullOrWhiteSpace(qrPayload.Signature))
        {
            return new QrCodePayloadValidationResult(false, null, null);
        }

        var expectedSignature = Sign(ticketId, orderId);
        var isValid = FixedTimeEquals(expectedSignature, qrPayload.Signature);

        return new QrCodePayloadValidationResult(isValid, ticketId, orderId);
    }

    private string Sign(Guid ticketId, Guid orderId)
    {
        var value = $"{ticketId:N}.{orderId:N}";
        var hash = HMACSHA256.HashData(_key, Encoding.UTF8.GetBytes(value));

        return ToBase64Url(hash);
    }

    private static bool FixedTimeEquals(string left, string right)
    {
        var leftBytes = Encoding.UTF8.GetBytes(left);
        var rightBytes = Encoding.UTF8.GetBytes(right);

        return CryptographicOperations.FixedTimeEquals(leftBytes, rightBytes);
    }

    private static string ToBase64Url(byte[] value) =>
        Convert.ToBase64String(value)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');

    private sealed record TicketQrPayload(string TicketId, string OrderId, string Signature);
}
