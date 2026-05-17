using Microsoft.Extensions.Options;

namespace TicketFlow.Infrastructure.Auth;

public sealed class JwtOptionsValidator : IValidateOptions<JwtOptions>
{
    private const string PlaceholderSigningKey = "replace-with-at-least-32-random-characters";
    private const int MinimumSigningKeyLength = 32;

    public ValidateOptionsResult Validate(string? name, JwtOptions options)
    {
        if (string.IsNullOrWhiteSpace(options.Issuer))
        {
            return ValidateOptionsResult.Fail("JWT issuer is required.");
        }

        if (string.IsNullOrWhiteSpace(options.Audience))
        {
            return ValidateOptionsResult.Fail("JWT audience is required.");
        }

        if (string.IsNullOrWhiteSpace(options.SigningKey))
        {
            return ValidateOptionsResult.Fail("JWT signing key is required.");
        }

        if (string.Equals(options.SigningKey, PlaceholderSigningKey, StringComparison.Ordinal))
        {
            return ValidateOptionsResult.Fail("JWT signing key must be replaced with a real secret.");
        }

        if (options.SigningKey.Length < MinimumSigningKeyLength)
        {
            return ValidateOptionsResult.Fail($"JWT signing key must be at least {MinimumSigningKeyLength} characters.");
        }

        return ValidateOptionsResult.Success;
    }
}
