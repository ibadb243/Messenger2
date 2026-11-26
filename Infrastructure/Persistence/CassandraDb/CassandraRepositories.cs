using Cassandra;
using Cassandra.Mapping;
using Messenger2.Application.Abstractions;
using Messenger2.Application.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Messenger2.Infrastructure.Persistence.CassandraDb;

public class CassandraUserReadRepository(IMapper mapper) : IUserReadRepository
{
    public async Task<UserById?> GetByIdAsync(int id) =>
        await mapper.SingleOrDefaultAsync<UserById>("WHERE id = ?", id);

    public Task UpsertAsync(UserById user) => mapper.InsertAsync(user);

    public Task DeleteAsync(int id) =>
        mapper.DeleteAsync<UserById>("WHERE id = ?", id);
}

public class CassandraChatReadRepository(IMapper mapper, ILogger<CassandraChatReadRepository> logger)
    : IChatReadRepository
{
    public async Task<ChatById?> GetByIdAsync(int chatId) =>
        await mapper.SingleOrDefaultAsync<ChatById>("WHERE id = ?", chatId);

    public async Task<IReadOnlyList<ChatById>> GetByUserAsync(int userId)
    {
        var links = await mapper.FetchAsync<ChatByUser>("WHERE user_id = ?", userId);
        var chats = await Task.WhenAll(links.Select(l => GetByIdAsync(l.ChatId)));
        return chats.Where(c => c is not null).Select(c => c!).ToList();
    }

    public Task UpsertAsync(ChatById chat) => mapper.InsertAsync(chat);

    public async Task DeleteAsync(int chatId)
    {
        logger.LogDebug("Deleting chat {ChatId} from Cassandra", chatId);

        // chats_by_user чистим по списку участников, пока он ещё есть в members_by_chat.
        var members = await mapper.FetchAsync<MemberByChat>("WHERE chat_id = ?", chatId);
        foreach (var m in members)
            await mapper.DeleteAsync<ChatByUser>("WHERE user_id = ? AND chat_id = ?", m.UserId, chatId);

        await mapper.DeleteAsync<MemberByChat>("WHERE chat_id = ?", chatId);
        await mapper.DeleteAsync<MessageByChat>("WHERE chat_id = ?", chatId);
        await mapper.DeleteAsync<ChatById>("WHERE id = ?", chatId);
    }
}

public class CassandraMemberReadRepository(IMapper mapper) : IMemberReadRepository
{
    public async Task<MemberByChat?> GetAsync(int chatId, int userId) =>
        await mapper.SingleOrDefaultAsync<MemberByChat>("WHERE chat_id = ? AND user_id = ?", chatId, userId);

    public async Task<IReadOnlyList<MemberByChat>> GetByChatAsync(int chatId) =>
        (await mapper.FetchAsync<MemberByChat>("WHERE chat_id = ?", chatId)).ToList();

    public async Task<IReadOnlyList<ChatByUser>> GetByUserAsync(int userId) =>
        (await mapper.FetchAsync<ChatByUser>("WHERE user_id = ?", userId)).ToList();

    public Task UpsertAsync(MemberByChat byChat, ChatByUser byUser)
    {
        var batch = mapper.CreateBatch(BatchType.Logged);
        batch.Insert(byChat);
        batch.Insert(byUser);
        return mapper.ExecuteAsync(batch);
    }

    public Task DeleteAsync(int userId, int chatId)
    {
        var batch = mapper.CreateBatch(BatchType.Logged);
        batch.Delete<MemberByChat>("WHERE chat_id = ? AND user_id = ?", chatId, userId);
        batch.Delete<ChatByUser>("WHERE user_id = ? AND chat_id = ?", userId, chatId);
        return mapper.ExecuteAsync(batch);
    }
}

public class CassandraMessageReadRepository(IMapper mapper) : IMessageReadRepository
{
    public async Task<MessageByChat?> GetAsync(int chatId, long id) =>
        await mapper.SingleOrDefaultAsync<MessageByChat>("WHERE chat_id = ? AND id = ?", chatId, id);

    public async Task<IReadOnlyList<MessageByChat>> GetByChatAsync(int chatId, long? beforeId, int limit)
    {
        var rows = beforeId is { } before
            ? await mapper.FetchAsync<MessageByChat>("WHERE chat_id = ? AND id < ? LIMIT ?", chatId, before, limit)
            : await mapper.FetchAsync<MessageByChat>("WHERE chat_id = ? LIMIT ?", chatId, limit);
        return rows.ToList();
    }

    public Task UpsertAsync(MessageByChat message) => mapper.InsertAsync(message);

    public Task DeleteAsync(int chatId, long id) =>
        mapper.DeleteAsync<MessageByChat>("WHERE chat_id = ? AND id = ?", chatId, id);
}

public class CassandraSyncStateRepository(IMapper mapper, ILogger<CassandraSyncStateRepository> logger)
    : ISyncStateRepository
{
    public async Task<Dictionary<string, long>> LoadVersionsAsync()
    {
        var rows = await mapper.FetchAsync<SyncState>();
        var versions = rows.ToDictionary(r => r.TableName, r => r.LastVersion);
        logger.LogDebug("Loaded saved versions: {@Versions}", versions);
        return versions;
    }

    public async Task<long?> GetVersionAsync(string table) =>
        (await mapper.SingleOrDefaultAsync<SyncState>("WHERE table_name = ?", table))?.LastVersion;

    public async Task SaveVersionAsync(string table, long version)
    {
        await mapper.InsertAsync(new SyncState { TableName = table, LastVersion = version });
        logger.LogDebug("Saved version of {Table}: {Version}", table, version);
    }

    public Task DeleteAsync(string table) =>
        mapper.DeleteAsync<SyncState>("WHERE table_name = ?", table);
}

public class CassandraReadStoreMaintenance(
    Cassandra.ISession session,
    IOptions<CassandraOptions> options,
    ILogger<CassandraReadStoreMaintenance> logger) : IReadStoreMaintenance
{
    private readonly string _keyspace = options.Value.Keyspace;

    public async Task TruncateAllAsync()
    {
        logger.LogWarning("Truncating all Cassandra tables (keyspace {Keyspace})", _keyspace);

        await TruncateAsync(CassandraTables.SyncState);
        foreach (var table in CassandraTables.Data)
            await TruncateAsync(table);
    }

    private async Task TruncateAsync(string table)
    {
        await session.ExecuteAsync(new SimpleStatement($"TRUNCATE {_keyspace}.{table}")
            .SetReadTimeoutMillis(options.Value.TruncateTimeoutMs));
        logger.LogDebug("Table {Table} truncated", table);
    }
}
