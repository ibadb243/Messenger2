using System.Security.Claims;
using System.Text;
using Messenger2.Application.Abstractions;
using Messenger2.Domain.Entities;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace Messenger2.Infrastructure.Security;

public sealed class JwtTokenService(IOptions<JwtOptions> options) : ITokenService
{
    private readonly JsonWebTokenHandler _handler = new();

    public AccessToken CreateToken(User user)
    {
        var jwt = options.Value;
        var expires = DateTimeOffset.UtcNow.AddMinutes(jwt.ExpiresMinutes);

        var token = _handler.CreateToken(new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(
            [
                new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
                new Claim(JwtRegisteredClaimNames.Email, user.Email),
            ]),
            Issuer = jwt.Issuer,
            Audience = jwt.Audience,
            Expires = expires.UtcDateTime,
            SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.Secret)), SecurityAlgorithms.HmacSha256),
        });

        return new AccessToken(token, expires);
    }
}
