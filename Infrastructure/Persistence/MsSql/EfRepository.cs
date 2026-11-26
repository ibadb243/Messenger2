using Messenger2.Application.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace Messenger2.Infrastructure.Persistence.MsSql;

/// <summary>Общая EF Core реализация CRUD. Конкретные репозитории задают ключ и порядок.</summary>
public abstract class EfRepository<TEntity, TKey>(MsSqlDbContext db) : IRepository<TEntity, TKey>
    where TEntity : class
{
    protected MsSqlDbContext Db { get; } = db;

    /// <summary>Базовый набор для запросов (наследники могут скрывать, например, удалённые записи).</summary>
    protected virtual IQueryable<TEntity> Query => Db.Set<TEntity>();

    protected abstract object?[] KeyValues(TKey id);
    protected abstract IOrderedQueryable<TEntity> Order(IQueryable<TEntity> query);

    public virtual async Task<TEntity?> GetByIdAsync(TKey id, CancellationToken ct = default) =>
        await Db.Set<TEntity>().FindAsync(KeyValues(id), ct);

    public virtual async Task<bool> ExistsAsync(TKey id, CancellationToken ct = default) =>
        await GetByIdAsync(id, ct) is not null;

    public virtual Task<int> CountAsync(CancellationToken ct = default) => Query.CountAsync(ct);

    public virtual async Task<IReadOnlyList<TEntity>> GetPageAsync(int offset, int limit, CancellationToken ct = default) =>
        await Order(Query.AsNoTracking()).Skip(offset).Take(limit).ToListAsync(ct);

    public virtual async Task<TEntity> AddAsync(TEntity entity, CancellationToken ct = default)
    {
        Db.Set<TEntity>().Add(entity);
        await Db.SaveChangesAsync(ct);
        return entity;
    }

    public virtual async Task UpdateAsync(TEntity entity, CancellationToken ct = default)
    {
        Db.Entry(entity).State = EntityState.Modified;
        await Db.SaveChangesAsync(ct);
    }

    public virtual async Task<bool> DeleteAsync(TKey id, CancellationToken ct = default)
    {
        var entity = await GetByIdAsync(id, ct);
        if (entity is null) return false;

        Db.Set<TEntity>().Remove(entity);
        await Db.SaveChangesAsync(ct);
        return true;
    }
}
