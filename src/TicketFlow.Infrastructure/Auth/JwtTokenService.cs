using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using TicketFlow.Application.Abstractions.Auth;
using TicketFlow.Application.Abstractions.Common;
using TicketFlow.Application.Features.Auth;
using TicketFlow.Domain.Entities;

namespace TicketFlow.Infrastructure.Auth;

internal sealed class JwtTokenService : ITokenService
{
    private readonly JwtOptions _jwtOptions;
    private readonly AuthOptions _authOptions;
    private readonly IClock _clock;

    public JwtTokenService(IOptions<JwtOptions> jwtOptions, IOptions<AuthOptions> authOptions, IClock clock)
    {
        _jwtOptions = jwtOptions.Value;
        _authOptions = authOptions.Value;
        _clock = clock;
    }

    public string CreateAccessToken(User user)
    {
        var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtOptions.SigningKey));
        var credentials = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, user.Email),
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Email, user.Email),
            new Claim(ClaimTypes.Role, user.Role.ToString())
        };

        var token = new JwtSecurityToken(
            _jwtOptions.Issuer,
            _jwtOptions.Audience,
            claims,
            notBefore: _clock.UtcNow,
            expires: _clock.UtcNow.AddMinutes(_authOptions.AccessTokenMinutes),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
