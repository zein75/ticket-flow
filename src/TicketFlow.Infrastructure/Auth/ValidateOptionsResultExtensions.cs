using Microsoft.Extensions.Options;

namespace TicketFlow.Infrastructure.Auth;

public static class ValidateOptionsResultExtensions
{
    public static void ThrowIfFailed(this ValidateOptionsResult result)
    {
        if (!result.Failed)
        {
            return;
        }

        throw new OptionsValidationException(
            JwtOptions.SectionName,
            typeof(JwtOptions),
            result.Failures);
    }
}
