using System.Security.Cryptography;
using TicketFlow.Application.Abstractions.Auth;

namespace TicketFlow.Infrastructure.Auth;

internal sealed class RefreshTokenGenerator : IRefreshTokenGenerator
{
    public string Generate()
    {
        return Base64UrlEncode(RandomNumberGenerator.GetBytes(64));
    }

    public string Hash(string refreshToken)
    {
        if (string.IsNullOrWhiteSpace(refreshToken))
        {
            throw new ArgumentException("Refresh token is required.", nameof(refreshToken));
        }

        var hash = SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(refreshToken));
        return Base64UrlEncode(hash);
    }

    private static string Base64UrlEncode(byte[] value)
    {
        return Convert.ToBase64String(value)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
    }
}
