using System.Security.Claims;
using Microsoft.IdentityModel.JsonWebTokens;

namespace Messenger2.Api.Infrastructure;

public static class ClaimsPrincipalExtensions
{
    /// <summary>Id текущего пользователя из claim sub (JWT уже провалидирован).</summary>
    public static int GetUserId(this ClaimsPrincipal principal) =>
        int.Parse(principal.FindFirstValue(JwtRegisteredClaimNames.Sub)
                  ?? throw new InvalidOperationException("В токене нет claim sub."));
}
