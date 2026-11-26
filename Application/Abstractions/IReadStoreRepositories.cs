using Messenger2.Application.Models;

namespace Messenger2.Application.Abstractions;

// Репозитории хранилища чтения (Cassandra).
// В Cassandra запись — всегда upsert, поэтому Create и Update объединены в UpsertAsync,
// а DeleteAsync идемпотентен (удаление несуществующей записи не ошибка).

public interface IUserReadRepository
{
    Task<UserById?> GetByIdAsync(int id);
    Task UpsertAsync(UserById user);
    Task DeleteAsync(int id);
}

public interface IChatReadRepository
{
    Task<ChatById?> GetByIdAsync(int chatId);

    /// <summary>Чаты пользователя (по денормализованной таблице chats_by_user).</summary>
    Task<IReadOnlyList<ChatById>> GetByUserAsync(int userId);

    Task UpsertAsync(ChatById chat);

    /// <summary>Удаляет чат вместе с участниками и сообщениями.</summary>
    Task DeleteAsync(int chatId);
}

public interface IMemberReadRepository
{
    Task<MemberByChat?> GetAsync(int chatId, int userId);
    Task<IReadOnlyList<MemberByChat>> GetByChatAsync(int chatId);

    /// <summary>Связи «пользователь — чат» (chats_by_user).</summary>
    Task<IReadOnlyList<ChatByUser>> GetByUserAsync(int userId);

    /// <summary>Записывает участника в обе денормализованные таблицы атомарно.</summary>
    Task UpsertAsync(MemberByChat byChat, ChatByUser byUser);

    /// <summary>Удаляет участника из обеих таблиц атомарно.</summary>
    Task DeleteAsync(int userId, int chatId);
}

public interface IMessageReadRepository
{
    Task<MessageByChat?> GetAsync(int chatId, long id);

    /// <summary>Сообщения чата от новых к старым; <paramref name="beforeId"/> — курсор для следующей страницы.</summary>
    Task<IReadOnlyList<MessageByChat>> GetByChatAsync(int chatId, long? beforeId, int limit);

    Task UpsertAsync(MessageByChat message);
    Task DeleteAsync(int chatId, long id);
}

/// <summary>Версии Change Tracking, до которых хранилище чтения синхронизировано.</summary>
public interface ISyncStateRepository
{
    Task<Dictionary<string, long>> LoadVersionsAsync();
    Task<long?> GetVersionAsync(string table);
    Task SaveVersionAsync(string table, long version);
    Task DeleteAsync(string table);
}

public interface IReadStoreMaintenance
{
    /// <summary>Полностью очищает хранилище чтения, включая состояние синхронизации.</summary>
    Task TruncateAllAsync();
}
