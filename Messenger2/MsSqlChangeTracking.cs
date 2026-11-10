using Microsoft.EntityFrameworkCore;

namespace Messenger2;

public class MsSqlChangeTracking(MsSqlDbContext db)
{
    public static readonly string[] Tables = ["Users", "Chats", "Members", "Messages"];

    public async Task<long> CurrentVersionAsync(CancellationToken ct)
    {
        var version = await db.Database
            .SqlQuery<long?>($"SELECT CHANGE_TRACKING_CURRENT_VERSION() AS Value")
            .SingleAsync(ct);

        return version ?? throw new InvalidOperationException("Change Tracking disabled");
    }

    public async Task<long> MinValidVersionAsync(string table, CancellationToken ct)
    {
        var objectName = "dbo." + table;
        var version = await db.Database
            .SqlQuery<long?>($"SELECT CHANGE_TRACKING_MIN_VALID_VERSION(OBJECT_ID({objectName})) AS Value")
            .SingleAsync(ct);

        return version ?? throw new InvalidOperationException($"Change Tracking disabled for {objectName} table");
    }

    public Task<List<UserChange>> UserChangesAsync(long since, CancellationToken ct) =>
        db.Database.SqlQuery<UserChange>($"""
            SELECT ct.id AS Id,
                   CAST(CASE WHEN u.id IS NULL THEN 0 ELSE 1 END AS bit) AS RowExists,
                   u.firstname AS FirstName, u.lastname AS LastName, u.avatar_url AS AvatarUrl,
                   u.email AS Email, u.created_at AS CreatedAt
            FROM CHANGETABLE(CHANGES dbo.Users, {since}) AS ct
            LEFT JOIN dbo.Users AS u ON u.id = ct.id
            """).ToListAsync(ct);

    public Task<List<ChatChange>> ChatChangesAsync(long since, CancellationToken ct) =>
        db.Database.SqlQuery<ChatChange>($"""
            SELECT ct.id AS Id,
                   CAST(CASE WHEN c.id IS NULL THEN 0 ELSE 1 END AS bit) AS RowExists,
                   c.name AS Name, c.avatar_url AS AvatarUrl, c.type AS Type,
                   c.created_by AS CreatedBy, c.created_at AS CreatedAt
            FROM CHANGETABLE(CHANGES dbo.Chats, {since}) AS ct
            LEFT JOIN dbo.Chats AS c ON c.id = ct.id
            """).ToListAsync(ct);

    public Task<List<MemberChange>> MemberChangesAsync(long since, CancellationToken ct) =>
        db.Database.SqlQuery<MemberChange>($"""
            SELECT ct.user_id AS UserId, ct.chat_id AS ChatId,
                   CAST(CASE WHEN m.user_id IS NULL THEN 0 ELSE 1 END AS bit) AS RowExists,
                   m.role AS Role, m.created_at AS CreatedAt
            FROM CHANGETABLE(CHANGES dbo.Members, {since}) AS ct
            LEFT JOIN dbo.Members AS m ON m.user_id = ct.user_id AND m.chat_id = ct.chat_id
            """).ToListAsync(ct);

    public Task<List<MessageChange>> MessageChangesAsync(long since, CancellationToken ct) =>
        db.Database.SqlQuery<MessageChange>($"""
            SELECT ct.id AS Id,
                   CAST(CASE WHEN m.id IS NULL THEN 0 ELSE 1 END AS bit) AS RowExists,
                   m.chat_id AS ChatId, m.sender_id AS SenderId, m.text AS Text,
                   m.created_at AS CreatedAt, m.edited_at AS EditedAt, m.deleted_at AS DeletedAt
            FROM CHANGETABLE(CHANGES dbo.Messages, {since}) AS ct
            LEFT JOIN dbo.Messages AS m ON m.id = ct.id
            """).ToListAsync(ct);

    public IAsyncEnumerable<User> AllUsers() => db.Users.AsNoTracking().AsAsyncEnumerable();
    public IAsyncEnumerable<Chat> AllChats() => db.Chats.AsNoTracking().AsAsyncEnumerable();
    public IAsyncEnumerable<Member> AllMembers() => db.Members.AsNoTracking().AsAsyncEnumerable();

    public IAsyncEnumerable<Message> AllLiveMessages() =>
        db.Messages.AsNoTracking().Where(m => m.DeletedAt == null).AsAsyncEnumerable();
}
