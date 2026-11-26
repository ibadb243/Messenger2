using Messenger2.Domain.Entities;

namespace Messenger2.Application.Abstractions;

public interface IUserRepository : IRepository<User, int>
{
    Task<User?> GetByEmailAsync(string email, CancellationToken ct = default);
    Task<bool> EmailExistsAsync(string email, CancellationToken ct = default);
}

public interface IChatRepository : IRepository<Chat, int>
{
    /// <summary>Чаты, в которых состоит пользователь.</summary>
    Task<IReadOnlyList<Chat>> GetByUserAsync(int userId, CancellationToken ct = default);
}

/// <summary>Участники чатов. Ключ — (UserId, ChatId).</summary>
public interface IMemberRepository : IRepository<Member, (int UserId, int ChatId)>
{
    Task<IReadOnlyList<Member>> GetByChatAsync(int chatId, CancellationToken ct = default);
    Task<IReadOnlyList<Member>> GetByUserAsync(int userId, CancellationToken ct = default);
}

/// <summary>
/// Сообщения. Удалённые (<see cref="Message.DeletedAt"/> != null) во всех запросах скрыты, а
/// <see cref="IRepository{TEntity,TKey}.DeleteAsync"/> — мягкое удаление: именно так удаление
/// доходит до синхронизации с Cassandra.
/// </summary>
public interface IMessageRepository : IRepository<Message, long>
{
    /// <summary>Сообщения чата от новых к старым; <paramref name="beforeId"/> — курсор для следующей страницы.</summary>
    Task<IReadOnlyList<Message>> GetByChatAsync(int chatId, long? beforeId, int limit, CancellationToken ct = default);
}
