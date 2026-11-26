using Messenger2.Application;
using Messenger2.Application.Abstractions;
using Messenger2.Application.Models;
using Messenger2.Domain.Entities;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Messenger2.Syncer;

public class Synchronizer(
    IChangeTrackingRepository changes,
    ISourceDataRepository source,
    ISyncStateRepository state,
    IReadStoreMaintenance maintenance,
    IUserReadRepository userRepo,
    IChatReadRepository chatRepo,
    IMemberReadRepository memberRepo,
    IMessageReadRepository messageRepo,
    IOptions<SyncOptions> options,
    ILogger<Synchronizer> logger)
{
    private readonly int _parallelism = options.Value.ParallelDegree;

    public async Task RunOnceAsync(CancellationToken cancellationToken)
    {
        var saved = await state.LoadVersionsAsync();

        await using var tx = await changes.BeginSnapshotAsync(cancellationToken);

        var current = await changes.CurrentVersionAsync(cancellationToken);

        var reloadReasons = new List<string>();
        foreach (var table in MsSqlTables.All)
        {
            var minValid = await changes.MinValidVersionAsync(table, cancellationToken);
            if (!saved.TryGetValue(table, out var last))
                reloadReasons.Add($"{table}: version missing");
            else if (last < minValid)
                reloadReasons.Add($"{table}: version {last} < min valid {minValid}");
        }

        if (reloadReasons.Count > 0)
        {
            logger.LogWarning("Full reload required: {Reasons}", string.Join("; ", reloadReasons));
            await FullReloadAsync(current, cancellationToken);
            await tx.CommitAsync(cancellationToken);
            return;
        }

        if (MsSqlTables.All.All(t => saved[t] == current))
        {
            logger.LogDebug("No changes, version {Version}", current);
            await tx.CommitAsync(cancellationToken);
            return;
        }

        var users = await changes.UserChangesAsync(saved[MsSqlTables.Users], cancellationToken);
        var chats = await changes.ChatChangesAsync(saved[MsSqlTables.Chats], cancellationToken);
        var members = await changes.MemberChangesAsync(saved[MsSqlTables.Members], cancellationToken);
        var messages = await changes.MessageChangesAsync(saved[MsSqlTables.Messages], cancellationToken);
        await tx.CommitAsync(cancellationToken);

        await ApplyAsync(users, ApplyUserAsync, cancellationToken);
        await state.SaveVersionAsync(MsSqlTables.Users, current);

        await ApplyAsync(chats, ApplyChatAsync, cancellationToken);
        await state.SaveVersionAsync(MsSqlTables.Chats, current);

        await ApplyAsync(members, ApplyMemberAsync, cancellationToken);
        await state.SaveVersionAsync(MsSqlTables.Members, current);

        var orphaned = messages.Count(m => !m.RowExists);
        await ApplyAsync(messages.Where(m => m.RowExists), ApplyMessageAsync, cancellationToken);
        await state.SaveVersionAsync(MsSqlTables.Messages, current);

        if (orphaned > 0) logger.LogDebug("Skipped {Count} physically deleted messages without chat_id (expected on chat cascade delete)", orphaned);

        logger.LogInformation(
            "Synced to version {Version}: users {Users}, chats {Chats}, members {Members}, messages {Messages}",
            current, users.Count, chats.Count, members.Count, messages.Count);
    }

    private Task ApplyUserAsync(UserChange c) =>
        c.RowExists
            ? userRepo.UpsertAsync(new UserById
            {
                Id = c.Id,
                FirstName = c.FirstName!,
                LastName = c.LastName!,
                AvatarUrl = c.AvatarUrl,
            })
            : userRepo.DeleteAsync(c.Id);

    private Task ApplyChatAsync(ChatChange c) =>
        c.RowExists
            ? chatRepo.UpsertAsync(new ChatById
            {
                Id = c.Id,
                Name = c.Name,
                AvatarUrl = c.AvatarUrl,
                Type = c.Type!,
                CreatedBy = c.CreatedBy!.Value,
                CreatedAt = c.CreatedAt!.Value,
            })
            : chatRepo.DeleteAsync(c.Id);

    private Task ApplyMemberAsync(MemberChange c) =>
        c.RowExists
            ? memberRepo.UpsertAsync(
                NewMemberByChat(c.ChatId, c.UserId, c.Role!, c.CreatedAt!.Value),
                NewChatByUser(c.UserId, c.ChatId, c.Role!, c.CreatedAt!.Value))
            : memberRepo.DeleteAsync(c.UserId, c.ChatId);

    private Task ApplyMessageAsync(MessageChange c) =>
        c.DeletedAt is not null
            ? messageRepo.DeleteAsync(c.ChatId!.Value, c.Id)
            : messageRepo.UpsertAsync(new MessageByChat
            {
                ChatId = c.ChatId!.Value,
                Id = c.Id,
                SenderId = c.SenderId!.Value,
                Text = c.Text!,
                CreatedAt = c.CreatedAt!.Value,
                EditedAt = c.EditedAt is { } e ? e : null,
            });

    private async Task FullReloadAsync(long version, CancellationToken cancellationToken)
    {
        await maintenance.TruncateAllAsync();

        await ApplyAsync(source.AllUsers(), u => userRepo.UpsertAsync(new UserById
        {
            Id = u.Id,
            FirstName = u.FirstName,
            LastName = u.LastName,
            AvatarUrl = u.AvatarUrl,
        }), cancellationToken);

        await ApplyAsync(source.AllChats(), c => chatRepo.UpsertAsync(new ChatById
        {
            Id = c.Id,
            Name = c.Name,
            AvatarUrl = c.AvatarUrl,
            Type = c.Type.ToString().ToLowerInvariant(),
            CreatedBy = c.CreatedBy,
            CreatedAt = c.CreatedAt,
        }), cancellationToken);

        await ApplyAsync(source.AllMembers(), m =>
        {
            var role = m.Role.ToString().ToLowerInvariant();
            return memberRepo.UpsertAsync(
                NewMemberByChat(m.ChatId, m.UserId, role, m.CreatedAt),
                NewChatByUser(m.UserId, m.ChatId, role, m.CreatedAt));
        }, cancellationToken);

        await ApplyAsync(source.AllLiveMessages(), m => messageRepo.UpsertAsync(new MessageByChat
        {
            ChatId = m.ChatId,
            Id = m.Id,
            SenderId = m.SenderId,
            Text = m.Text,
            CreatedAt = m.CreatedAt,
            EditedAt = m.EditedAt is { } e ? e : null,
        }), cancellationToken);

        // Версии пишем последними: пока их нет, незавершённая перезагрузка повторится.
        foreach (var table in MsSqlTables.All)
            await state.SaveVersionAsync(table, version);

        logger.LogInformation("Full reload completed, version {Version}", version);
    }

    private Task ApplyAsync<T>(IEnumerable<T> items, Func<T, Task> apply, CancellationToken ct) =>
        Parallel.ForEachAsync(items, Options(ct), async (item, _) => await apply(item));

    private Task ApplyAsync<T>(IAsyncEnumerable<T> items, Func<T, Task> apply, CancellationToken ct) =>
        Parallel.ForEachAsync(items, Options(ct), async (item, _) => await apply(item));

    private ParallelOptions Options(CancellationToken ct) =>
        new() { MaxDegreeOfParallelism = _parallelism, CancellationToken = ct };

    private static MemberByChat NewMemberByChat(int chatId, int userId, string role, DateTimeOffset createdAt) =>
        new() { ChatId = chatId, UserId = userId, Role = role, CreatedAt = createdAt };

    private static ChatByUser NewChatByUser(int userId, int chatId, string role, DateTimeOffset createdAt) =>
        new() { UserId = userId, ChatId = chatId };
}