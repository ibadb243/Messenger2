using Messenger2.Application.Abstractions;
using Messenger2.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Messenger2.Infrastructure.Persistence.MsSql;

public class UserRepository(MsSqlDbContext db) : EfRepository<User, int>(db), IUserRepository
{
    protected override object?[] KeyValues(int id) => [id];
    protected override IOrderedQueryable<User> Order(IQueryable<User> q) => q.OrderBy(x => x.Id);

    public Task<User?> GetByEmailAsync(string email, CancellationToken ct = default) =>
        Db.Users.FirstOrDefaultAsync(u => u.Email == email, ct);

    public Task<bool> EmailExistsAsync(string email, CancellationToken ct = default) =>
        Db.Users.AnyAsync(u => u.Email == email, ct);

    public async Task<IReadOnlyList<User>> SearchAsync(string query, int limit, CancellationToken ct = default) =>
        await Db.Users.AsNoTracking()
            .Where(u => u.FirstName.Contains(query) || u.LastName.Contains(query) || u.Email.Contains(query))
            .OrderBy(u => u.FirstName).ThenBy(u => u.LastName)
            .Take(limit)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<User>> GetByIdsAsync(IReadOnlyCollection<int> ids, CancellationToken ct = default) =>
        await Db.Users.AsNoTracking().Where(u => ids.Contains(u.Id)).OrderBy(u => u.Id).ToListAsync(ct);
}

public class ChatRepository(MsSqlDbContext db) : EfRepository<Chat, int>(db), IChatRepository
{
    protected override object?[] KeyValues(int id) => [id];
    protected override IOrderedQueryable<Chat> Order(IQueryable<Chat> q) => q.OrderBy(x => x.Id);

    public async Task<IReadOnlyList<Chat>> GetByUserAsync(int userId, CancellationToken ct = default) =>
        await Db.Members.AsNoTracking()
            .Where(m => m.UserId == userId)
            .Select(m => m.Chat)
            .OrderBy(c => c.Id)
            .ToListAsync(ct);

    public Task<Chat?> FindPrivateChatAsync(int userA, int userB, CancellationToken ct = default) =>
        Db.Chats.AsNoTracking().FirstOrDefaultAsync(c =>
            c.Type == ChatType.Private
            && c.Members.Any(m => m.UserId == userA)
            && c.Members.Any(m => m.UserId == userB), ct);
}

public class MemberRepository(MsSqlDbContext db)
    : EfRepository<Member, (int UserId, int ChatId)>(db), IMemberRepository
{
    protected override object?[] KeyValues((int UserId, int ChatId) id) => [id.UserId, id.ChatId];

    protected override IOrderedQueryable<Member> Order(IQueryable<Member> q) =>
        q.OrderBy(x => x.ChatId).ThenBy(x => x.UserId);

    public async Task<IReadOnlyList<Member>> GetByChatAsync(int chatId, CancellationToken ct = default) =>
        await Db.Members.AsNoTracking().Where(m => m.ChatId == chatId).OrderBy(m => m.UserId).ToListAsync(ct);

    public async Task<IReadOnlyList<Member>> GetByUserAsync(int userId, CancellationToken ct = default) =>
        await Db.Members.AsNoTracking().Where(m => m.UserId == userId).OrderBy(m => m.ChatId).ToListAsync(ct);
}

public class MessageRepository(MsSqlDbContext db) : EfRepository<Message, long>(db), IMessageRepository
{
    protected override IQueryable<Message> Query => Db.Messages.Where(m => m.DeletedAt == null);

    protected override object?[] KeyValues(long id) => [id];
    protected override IOrderedQueryable<Message> Order(IQueryable<Message> q) => q.OrderBy(x => x.Id);

    public override async Task<Message?> GetByIdAsync(long id, CancellationToken ct = default)
    {
        var message = await base.GetByIdAsync(id, ct);
        return message is { DeletedAt: null } ? message : null;
    }

    /// <summary>Мягкое удаление: проставляет DeletedAt, чтобы удаление попало в синхронизацию.</summary>
    public override async Task<bool> DeleteAsync(long id, CancellationToken ct = default)
    {
        var message = await GetByIdAsync(id, ct);
        if (message is null) return false;

        message.DeletedAt = DateTimeOffset.UtcNow;
        await Db.SaveChangesAsync(ct);
        return true;
    }

    public async Task<IReadOnlyList<Message>> GetByChatAsync(
        int chatId, long? beforeId, int limit, CancellationToken ct = default) =>
        await Query.AsNoTracking()
            .Where(m => m.ChatId == chatId && (beforeId == null || m.Id < beforeId))
            .OrderByDescending(m => m.Id)
            .Take(limit)
            .ToListAsync(ct);
}
