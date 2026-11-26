using System.Security.Claims;
using Messenger2.Api.Infrastructure;
using Messenger2.Application.Contracts;
using Messenger2.Application.Services;

namespace Messenger2.Api.Endpoints;

public static class ChatEndpoints
{
    public static void MapChatEndpoints(this IEndpointRouteBuilder app)
    {
        var chats = app.MapGroup("/api/chats").WithTags("Chats").RequireAuthorization();

        chats.MapGet("/", async (ClaimsPrincipal user, ChatService service, CancellationToken ct) =>
            Results.Ok(await service.GetMyChatsAsync(user.GetUserId(), ct)));

        chats.MapGet("/{chatId:int}", async (int chatId, ClaimsPrincipal user, ChatService service, CancellationToken ct) =>
            Results.Ok(await service.GetAsync(chatId, user.GetUserId(), ct)));

        chats.MapPost("/private", async (CreatePrivateChatRequest request, ClaimsPrincipal user, ChatService service, CancellationToken ct) =>
            Results.Ok(await service.CreatePrivateAsync(user.GetUserId(), request, ct)));

        chats.MapPost("/group", async (CreateGroupChatRequest request, ClaimsPrincipal user, ChatService service, CancellationToken ct) =>
            Results.Ok(await service.CreateGroupAsync(user.GetUserId(), request, ct)));

        chats.MapPost("/{chatId:int}/members", async (int chatId, AddMemberRequest request, ClaimsPrincipal user, ChatService service, CancellationToken ct) =>
            Results.Ok(await service.AddMemberAsync(chatId, user.GetUserId(), request, ct)));

        chats.MapDelete("/{chatId:int}/members/{userId:int}", async (int chatId, int userId, ClaimsPrincipal user, ChatService service, CancellationToken ct) =>
        {
            await service.RemoveMemberAsync(chatId, user.GetUserId(), userId, ct);
            return Results.NoContent();
        });

        var messages = chats.MapGroup("/{chatId:int}/messages");

        messages.MapGet("/", async (int chatId, long? before, int? limit, ClaimsPrincipal user, MessageService service, CancellationToken ct) =>
            Results.Ok(await service.GetHistoryAsync(chatId, user.GetUserId(), before, limit, ct)));

        messages.MapPost("/", async (int chatId, SendMessageRequest request, ClaimsPrincipal user, MessageService service, CancellationToken ct) =>
            Results.Ok(await service.SendAsync(chatId, user.GetUserId(), request, ct)));

        messages.MapPut("/{messageId:long}", async (int chatId, long messageId, EditMessageRequest request, ClaimsPrincipal user, MessageService service, CancellationToken ct) =>
            Results.Ok(await service.EditAsync(chatId, messageId, user.GetUserId(), request, ct)));

        messages.MapDelete("/{messageId:long}", async (int chatId, long messageId, ClaimsPrincipal user, MessageService service, CancellationToken ct) =>
        {
            await service.DeleteAsync(chatId, messageId, user.GetUserId(), ct);
            return Results.NoContent();
        });
    }
}
