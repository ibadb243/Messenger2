using Messenger2.Application.Abstractions;
using Messenger2.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Messenger2.Infrastructure.Persistence.MsSql;

/// <summary>Реализация <see cref="ISourceDataRepository"/> поверх EF Core.</summary>
public class MsSqlSourceDataRepository(MsSqlDbContext db) : ISourceDataRepository
{
    public IAsyncEnumerable<User> AllUsers() => db.Users.AsNoTracking().AsAsyncEnumerable();
    public IAsyncEnumerable<Chat> AllChats() => db.Chats.AsNoTracking().AsAsyncEnumerable();
    public IAsyncEnumerable<Member> AllMembers() => db.Members.AsNoTracking().AsAsyncEnumerable();

    public IAsyncEnumerable<Message> AllLiveMessages() =>
        db.Messages.AsNoTracking().Where(m => m.DeletedAt == null).AsAsyncEnumerable();
}
