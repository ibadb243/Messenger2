using Cassandra;
using Cassandra.Mapping;
using Microsoft.Extensions.Options;

namespace Messenger2;

public class CassandraStore(
    Cassandra.ISession session,
    IOptions<CassandraOptions> options,
    ILogger<CassandraStore> logger)
{
    private readonly IMapper _mapper = new Mapper(session);
    private readonly string _keyspace = options.Value.Keyspace;

    public async Task<Dictionary<string, long>> LoadVersionsAsync()
    {
        var rows = await _mapper.FetchAsync<SyncState>();
        var versions = rows.ToDictionary(r => r.TableName, r => r.LastVersion);
        logger.LogDebug("Loaded saved versions: {@Versions}", versions);
        return versions;
    }

    public async Task SaveVersionAsync(string table, long version)
    {
        await _mapper.InsertAsync(new SyncState { TableName = table, LastVersion = version });
        logger.LogDebug("Saved version of {Table}: {Version}", table, version);
    }

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

    public Task UpsertUserAsync(UserById user) => _mapper.InsertAsync(user);

    public Task DeleteUserAsync(int id) =>
        _mapper.DeleteAsync<UserById>("WHERE id = ?", id);

    public Task UpsertChatAsync(ChatById chat) => _mapper.InsertAsync(chat);

    public async Task DeleteChatAsync(int chatId)
    {
        logger.LogDebug("Deleting chat {ChatId} from Cassandra", chatId);

        // chats_by_user чистим по списку участников, пока он ещё есть в members_by_chat.
        var members = await _mapper.FetchAsync<MemberByChat>("WHERE chat_id = ?", chatId);
        foreach (var m in members)
            await _mapper.DeleteAsync<ChatByUser>("WHERE user_id = ? AND chat_id = ?", m.UserId, chatId);

        await _mapper.DeleteAsync<MemberByChat>("WHERE chat_id = ?", chatId);
        await _mapper.DeleteAsync<MessageByChat>("WHERE chat_id = ?", chatId);
        await _mapper.DeleteAsync<ChatById>("WHERE id = ?", chatId);
    }

    public Task UpsertMemberAsync(MemberByChat byChat, ChatByUser byUser)
    {
        var batch = _mapper.CreateBatch(BatchType.Logged);
        batch.Insert(byChat);
        batch.Insert(byUser);
        return _mapper.ExecuteAsync(batch);
    }

    public Task DeleteMemberAsync(int userId, int chatId)
    {
        var batch = _mapper.CreateBatch(BatchType.Logged);
        batch.Delete<MemberByChat>("WHERE chat_id = ? AND user_id = ?", chatId, userId);
        batch.Delete<ChatByUser>("WHERE user_id = ? AND chat_id = ?", userId, chatId);
        return _mapper.ExecuteAsync(batch);
    }

    public Task UpsertMessageAsync(MessageByChat message) => _mapper.InsertAsync(message);

    public Task DeleteMessageAsync(int chatId, long id) =>
        _mapper.DeleteAsync<MessageByChat>("WHERE chat_id = ? AND id = ?", chatId, id);
}
