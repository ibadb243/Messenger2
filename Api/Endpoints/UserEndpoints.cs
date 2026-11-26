using System.Security.Claims;
using Messenger2.Api.Infrastructure;
using Messenger2.Application.Contracts;
using Messenger2.Application.Services;

namespace Messenger2.Api.Endpoints;

public static class UserEndpoints
{
    public static void MapUserEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/users").WithTags("Users").RequireAuthorization();

        group.MapGet("/me", async (ClaimsPrincipal user, UserService users, CancellationToken ct) =>
            Results.Ok(await users.GetMeAsync(user.GetUserId(), ct)));

        group.MapPut("/me", async (UpdateProfileRequest request, ClaimsPrincipal user, UserService users, CancellationToken ct) =>
            Results.Ok(await users.UpdateProfileAsync(user.GetUserId(), request, ct)));

        group.MapGet("/search", async (string? q, ClaimsPrincipal user, UserService users, CancellationToken ct) =>
            Results.Ok(await users.SearchAsync(q, user.GetUserId(), ct)));

        group.MapGet("/{id:int}", async (int id, UserService users, CancellationToken ct) =>
            Results.Ok(await users.GetByIdAsync(id, ct)));
    }
}
