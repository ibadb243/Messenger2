namespace Messenger2.Infrastructure.Persistence.CassandraDb;

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
