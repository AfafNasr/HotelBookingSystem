using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using HotelBooking.Application.Authentication;
using HotelBooking.Application.Common.Models;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace HotelBooking.Infrastructure.Authentication;

public sealed class JwtTokenService : ITokenService
{
    private readonly JwtOptions _options;

    public JwtTokenService(IOptions<JwtOptions> options)
    {
        _options = options.Value;
    }

    public AccessToken CreateToken(AuthenticatedUser user)
    {
        var now = DateTimeOffset.UtcNow;
        var expiresAt = now.AddMinutes(_options.ExpirationMinutes);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.UserId),
            new(ClaimTypes.Name, user.Username)
        };

        claims.AddRange(
            user.Roles.Select(role =>
                new Claim(ClaimTypes.Role, role)));

        var signingKey = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(_options.Key));

        var signingCredentials = new SigningCredentials(
            signingKey,
            SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _options.Issuer,
            audience: _options.Audience,
            claims: claims,
            notBefore: now.UtcDateTime,
            expires: expiresAt.UtcDateTime,
            signingCredentials: signingCredentials);

        var tokenValue =
            new JwtSecurityTokenHandler().WriteToken(token);

        return new AccessToken(
            tokenValue,
            expiresAt);
    }
}