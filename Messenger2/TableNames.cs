namespace Messenger2;

/// <summary>Таблицы MS SQL, отслеживаемые Change Tracking (они же ключи в sync_state).</summary>
public static class MsSqlTables
{
    public const string Schema = "dbo";

    public const string Users = "Users";
    public const string Chats = "Chats";
    public const string Members = "Members";
    public const string Messages = "Messages";

    /// <summary>Порядок важен: он же порядок применения изменений.</summary>
    public static readonly string[] All = [Users, Chats, Members, Messages];
}

public static class CassandraTables
{
    public const string UsersById = "users_by_id";
    public const string ChatsById = "chats_by_id";
    public const string MembersByChat = "members_by_chat";
    public const string ChatsByUser = "chats_by_user";
    public const string MessagesByChat = "messages_by_chat";
    public const string SyncState = "sync_state";

    public static readonly string[] Data =
        [UsersById, ChatsById, MembersByChat, ChatsByUser, MessagesByChat];
}
