using System.Linq.Expressions;

namespace ERMS.Application.Abstractions.Persistence;

/// <summary>
/// Basit CRUD işlemleri için generic repository sözleşmesi.
/// Karmaşık/çok tablolu sorgular için entity'ye özel query repository'leri kullanılmalıdır
/// (örn. <see cref="IRequestQueryRepository"/>) — bkz. proje mimari notları.
/// </summary>
public interface IRepository<TEntity>
    where TEntity : class
{
    /// <summary>İleri seviye (Include/filtre) sorgular için ham IQueryable erişimi.</summary>
    IQueryable<TEntity> Query();

    Task<TEntity?> GetByIdAsync(
        int id,
        CancellationToken cancellationToken = default);

    Task<TEntity?> FirstOrDefaultAsync(
        Expression<Func<TEntity, bool>> predicate,
        CancellationToken cancellationToken = default);

    Task<List<TEntity>> GetAllAsync(
        CancellationToken cancellationToken = default);

    Task<List<TEntity>> FindAsync(
        Expression<Func<TEntity, bool>> predicate,
        CancellationToken cancellationToken = default);

    Task AddAsync(
        TEntity entity,
        CancellationToken cancellationToken = default);

    Task AddRangeAsync(
        IEnumerable<TEntity> entities,
        CancellationToken cancellationToken = default);

    void Update(TEntity entity);

    void Remove(TEntity entity);
}
