using System.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Messenger2;

public class Synchronizer(
    MsSqlDbContext db,
    MsSqlChangeTracking ct,
    CassandraStore store,
    IOptions<SyncOptions> options,
    ILogger<Synchronizer> logger)
{
    private readonly int _parallelism = 5;

    public async Task RunOnceAsync(CancellationToken cancellationToken)
    {
        var saved = await store.LoadVersionsAsync();

        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Snapshot, cancellationToken);

        var current = await ct.CurrentVersionAsync(cancellationToken);

        var reloadReasons = new List<string>();
        foreach (var table in MsSqlChangeTracking.Tables)
        {
            var minValid = await ct.MinValidVersionAsync(table, cancellationToken);
            if (!saved.TryGetValue(table, out var last))
                reloadReasons.Add($"{table}: версии нет");
            else if (last < minValid)
                reloadReasons.Add($"{table}: версия {last} < минимально допустимой {minValid}");
        }

        if (reloadReasons.Count > 0)
        {
            logger.LogWarning("Полная перезагрузка ({Reasons})", string.Join("; ", reloadReasons));
            await FullReloadAsync(current, cancellationToken);
            await tx.CommitAsync(cancellationToken);
            return;
        }

        if (MsSqlChangeTracking.Tables.All(t => saved[t] == current))
        {
            await tx.CommitAsync(cancellationToken);
            return;
        }

        var users = await ct.UserChangesAsync(saved["Users"], cancellationToken);
        var chats = await ct.ChatChangesAsync(saved["Chats"], cancellationToken);
        var members = await ct.MemberChangesAsync(saved["Members"], cancellationToken);
        var messages = await ct.MessageChangesAsync(saved["Messages"], cancellationToken);
        await tx.CommitAsync(cancellationToken);

        await ApplyAsync(users, ApplyUserAsync, cancellationToken);
        await store.SaveVersionAsync("Users", current);

        await ApplyAsync(chats, ApplyChatAsync, cancellationToken);
        await store.SaveVersionAsync("Chats", current);

        await ApplyAsync(members, ApplyMemberAsync, cancellationToken);
        await store.SaveVersionAsync("Members", current);

        var orphaned = messages.Count(m => !m.RowExists);
        await ApplyAsync(messages.Where(m => m.RowExists), ApplyMessageAsync, cancellationToken);
        await store.SaveVersionAsync("Messages", current);

        if (orphaned > 0) logger.LogDebug("Пропущено {Count} физически удалённых сообщений без chat_id " +
                         "(ожидаемо при каскадном удалении чата)", orphaned);

        logger.LogInformation("Version {Version}: users {U}, chats {C}, members {M}, messages {Msg}",
            current, users.Count, chats.Count, members.Count, messages.Count);
    }

    private Task ApplyUserAsync(UserChange c) =>
        c.RowExists
            ? store.UpsertUserAsync(new UserById
            {
                Id = c.Id,
                FirstName = c.FirstName!,
                LastName = c.LastName!,
                AvatarUrl = c.AvatarUrl,
            })
            : store.DeleteUserAsync(c.Id);

    private Task ApplyChatAsync(ChatChange c) =>
        c.RowExists
            ? store.UpsertChatAsync(new ChatById
            {
                Id = c.Id,
                Name = c.Name,
                AvatarUrl = c.AvatarUrl,
                Type = c.Type!,
                CreatedBy = c.CreatedBy!.Value,
                CreatedAt = c.CreatedAt!.Value,
            })
            : store.DeleteChatAsync(c.Id);

    private Task ApplyMemberAsync(MemberChange c) =>
        c.RowExists
            ? store.UpsertMemberAsync(
                NewMemberByChat(c.ChatId, c.UserId, c.Role!, c.CreatedAt!.Value),
                NewChatByUser(c.UserId, c.ChatId, c.Role!, c.CreatedAt!.Value))
            : store.DeleteMemberAsync(c.UserId, c.ChatId);

    private Task ApplyMessageAsync(MessageChange c) =>
        c.DeletedAt is not null
            ? store.DeleteMessageAsync(c.ChatId!.Value, c.Id)
            : store.UpsertMessageAsync(new MessageByChat
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
        await store.TruncateAllAsync();

        await ApplyAsync(ct.AllUsers(), u => store.UpsertUserAsync(new UserById
        {
            Id = u.Id,
            FirstName = u.FirstName,
            LastName = u.LastName,
            AvatarUrl = u.AvatarUrl,
        }), cancellationToken);

        await ApplyAsync(ct.AllChats(), c => store.UpsertChatAsync(new ChatById
        {
            Id = c.Id,
            Name = c.Name,
            AvatarUrl = c.AvatarUrl,
            Type = c.Type.ToString().ToLowerInvariant(),
            CreatedBy = c.CreatedBy,
            CreatedAt = c.CreatedAt,
        }), cancellationToken);

        await ApplyAsync(ct.AllMembers(), m =>
        {
            var role = m.Role.ToString().ToLowerInvariant();
            return store.UpsertMemberAsync(
                NewMemberByChat(m.ChatId, m.UserId, role, m.CreatedAt),
                NewChatByUser(m.UserId, m.ChatId, role, m.CreatedAt));
        }, cancellationToken);

        await ApplyAsync(ct.AllLiveMessages(), m => store.UpsertMessageAsync(new MessageByChat
        {
            ChatId = m.ChatId,
            Id = m.Id,
            SenderId = m.SenderId,
            Text = m.Text,
            CreatedAt = m.CreatedAt,
            EditedAt = m.EditedAt is { } e ? e : null,
        }), cancellationToken);

        // Версии пишем последними: пока их нет, незавершённая перезагрузка повторится.
        foreach (var table in MsSqlChangeTracking.Tables)
            await store.SaveVersionAsync(table, version);

        logger.LogInformation("{Version}", version);
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