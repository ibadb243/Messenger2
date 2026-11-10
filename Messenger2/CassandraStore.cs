using Cassandra;
using Cassandra.Mapping;

namespace Messenger2;

public class CassandraStore(Cassandra.ISession session)
{
    private readonly IMapper _mapper = new Mapper(session);

    private static readonly string[] DataTables =
        ["users_by_id", "chats_by_id", "members_by_chat", "chats_by_user", "messages_by_chat"];

    public async Task<Dictionary<string, long>> LoadVersionsAsync()
    {
        var rows = await _mapper.FetchAsync<SyncState>();
        return rows.ToDictionary(r => r.TableName, r => r.LastVersion);
    }

    public Task SaveVersionAsync(string table, long version) =>
        _mapper.InsertAsync(new SyncState { TableName = table, LastVersion = version });

    public async Task TruncateAllAsync()
    {
        await TruncateAsync("sync_state");
        foreach (var table in DataTables)
            await TruncateAsync(table);
    }

    private Task TruncateAsync(string table) =>
        session.ExecuteAsync(new SimpleStatement($"TRUNCATE messenger.{table}")
            .SetReadTimeoutMillis(60_000));

    public Task UpsertUserAsync(UserById user) => _mapper.InsertAsync(user);

    public Task DeleteUserAsync(int id) =>
        _mapper.DeleteAsync<UserById>("WHERE id = ?", id);

    public Task UpsertChatAsync(ChatById chat) => _mapper.InsertAsync(chat);

    public async Task DeleteChatAsync(int chatId)
    {
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
