namespace Messenger2.Application.Abstractions;

/// <summary>
/// Базовый CRUD-репозиторий доменных сущностей MS SQL.
/// Каждая операция записи сразу сохраняется в БД (отдельный unit of work не нужен).
/// </summary>
/// <typeparam name="TEntity">Доменная сущность.</typeparam>
/// <typeparam name="TKey">Тип первичного ключа (для составного — кортеж).</typeparam>
public interface IRepository<TEntity, in TKey> where TEntity : class
{
    // Read
    Task<TEntity?> GetByIdAsync(TKey id, CancellationToken ct = default);
    Task<bool> ExistsAsync(TKey id, CancellationToken ct = default);
    Task<int> CountAsync(CancellationToken ct = default);

    /// <summary>Страница сущностей в стабильном порядке по ключу.</summary>
    Task<IReadOnlyList<TEntity>> GetPageAsync(int offset, int limit, CancellationToken ct = default);

    // Create
    /// <summary>Добавляет сущность и возвращает её с заполненными серверными значениями (Id, CreatedAt).</summary>
    Task<TEntity> AddAsync(TEntity entity, CancellationToken ct = default);

    // Update
    /// <summary>Сохраняет все скалярные свойства сущности.</summary>
    Task UpdateAsync(TEntity entity, CancellationToken ct = default);

    // Delete
    /// <returns><c>true</c>, если сущность существовала и была удалена.</returns>
    Task<bool> DeleteAsync(TKey id, CancellationToken ct = default);
}
