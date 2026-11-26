using Messenger2.Application.Models;
using Cassandra.Mapping;

namespace Messenger2.Infrastructure.Persistence.CassandraDb;

public class CassandraMappings : Mappings
{
    public CassandraMappings(string keyspace)
    {
        For<UserById>()
            .KeyspaceName(keyspace).TableName(CassandraTables.UsersById)
            .PartitionKey(x => x.Id)
            .ExplicitColumns()
            .Column(x => x.Id, c => c.WithName("id"))
            .Column(x => x.FirstName, c => c.WithName("firstname"))
            .Column(x => x.LastName, c => c.WithName("lastname"))
            .Column(x => x.AvatarUrl, c => c.WithName("avatar_url"));

        For<ChatById>()
            .KeyspaceName(keyspace).TableName(CassandraTables.ChatsById)
            .PartitionKey(x => x.Id)
            .ExplicitColumns()
            .Column(x => x.Id, c => c.WithName("id"))
            .Column(x => x.Name, c => c.WithName("name"))
            .Column(x => x.AvatarUrl, c => c.WithName("avatar_url"))
            .Column(x => x.Type, c => c.WithName("type"))
            .Column(x => x.CreatedBy, c => c.WithName("created_by"))
            .Column(x => x.CreatedAt, c => c.WithName("created_at"));

        For<MemberByChat>()
            .KeyspaceName(keyspace).TableName(CassandraTables.MembersByChat)
            .PartitionKey(x => x.ChatId)
            .ClusteringKey(x => x.UserId)
            .ExplicitColumns()
            .Column(x => x.ChatId, c => c.WithName("chat_id"))
            .Column(x => x.UserId, c => c.WithName("user_id"))
            .Column(x => x.Role, c => c.WithName("role"))
            .Column(x => x.CreatedAt, c => c.WithName("created_at"));

        For<ChatByUser>()
            .KeyspaceName(keyspace).TableName(CassandraTables.ChatsByUser)
            .PartitionKey(x => x.UserId)
            .ClusteringKey(x => x.ChatId)
            .ExplicitColumns()
            .Column(x => x.UserId, c => c.WithName("user_id"))
            .Column(x => x.ChatId, c => c.WithName("chat_id"));

        For<MessageByChat>()
            .KeyspaceName(keyspace).TableName(CassandraTables.MessagesByChat)
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
            .KeyspaceName(keyspace).TableName(CassandraTables.SyncState)
            .PartitionKey(x => x.TableName)
            .ExplicitColumns()
            .Column(x => x.TableName, c => c.WithName("table_name"))
            .Column(x => x.LastVersion, c => c.WithName("last_version"));
    }
}