using Messenger2.Application.Contracts;
using Messenger2.Application.Services;

namespace Messenger2.Api.Endpoints;

public static class AuthEndpoints
{
    public static void MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/auth").WithTags("Auth").AllowAnonymous();

        group.MapPost("/register", async (RegisterRequest request, AuthService auth, CancellationToken ct) =>
            Results.Ok(await auth.RegisterAsync(request, ct)));

        group.MapPost("/login", async (LoginRequest request, AuthService auth, CancellationToken ct) =>
            Results.Ok(await auth.LoginAsync(request, ct)));
    }
}
