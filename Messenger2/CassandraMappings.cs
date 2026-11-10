using Cassandra.Mapping;

namespace Messenger2;

public class CassandraMappings : Mappings
{
    private const string Keyspace = "messenger";

    public CassandraMappings()
    {
        For<UserById>()
            .KeyspaceName(Keyspace).TableName("users_by_id")
            .PartitionKey(x => x.Id)
            .ExplicitColumns()
            .Column(x => x.Id, c => c.WithName("id"))
            .Column(x => x.FirstName, c => c.WithName("firstname"))
            .Column(x => x.LastName, c => c.WithName("lastname"))
            .Column(x => x.AvatarUrl, c => c.WithName("avatar_url"));

        For<ChatById>()
            .KeyspaceName(Keyspace).TableName("chats_by_id")
            .PartitionKey(x => x.Id)
            .ExplicitColumns()
            .Column(x => x.Id, c => c.WithName("id"))
            .Column(x => x.Name, c => c.WithName("name"))
            .Column(x => x.AvatarUrl, c => c.WithName("avatar_url"))
            .Column(x => x.Type, c => c.WithName("type"))
            .Column(x => x.CreatedBy, c => c.WithName("created_by"))
            .Column(x => x.CreatedAt, c => c.WithName("created_at"));

        For<MemberByChat>()
            .KeyspaceName(Keyspace).TableName("members_by_chat")
            .PartitionKey(x => x.ChatId)
            .ClusteringKey(x => x.UserId)
            .ExplicitColumns()
            .Column(x => x.ChatId, c => c.WithName("chat_id"))
            .Column(x => x.UserId, c => c.WithName("user_id"))
            .Column(x => x.Role, c => c.WithName("role"))
            .Column(x => x.CreatedAt, c => c.WithName("created_at"));

        For<ChatByUser>()
            .KeyspaceName(Keyspace).TableName("chats_by_user")
            .PartitionKey(x => x.UserId)
            .ClusteringKey(x => x.ChatId)
            .ExplicitColumns()
            .Column(x => x.UserId, c => c.WithName("user_id"))
            .Column(x => x.ChatId, c => c.WithName("chat_id"));

        For<MessageByChat>()
            .KeyspaceName(Keyspace).TableName("messages_by_chat")
            .PartitionKey(x => x.ChatId)
            .ClusteringKey(x => x.Id, SortOrder.Descending)
            .ExplicitColumns()
            .Column(x => x.ChatId, c => c.WithName("chat_id"))
            .Column(x => x.Id, c => c.WithName("id"))
            .Column(x => x.SenderId, c => c.WithName("sender_id"))
            .Column(x => x.Text, c => c.WithName("text"))
            .Column(x => x.CreatedAt, c => c.WithName("created_at"))
            .Column(x => x.EditedAt, c => c.WithName("edited_at"));

        For<SyncState>()
            .KeyspaceName(Keyspace).TableName("sync_state")
            .PartitionKey(x => x.TableName)
            .ExplicitColumns()
            .Column(x => x.TableName, c => c.WithName("table_name"))
            .Column(x => x.LastVersion, c => c.WithName("last_version"));
    }
}