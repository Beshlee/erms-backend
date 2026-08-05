using System.Linq.Expressions;
using ERMS.Application.Abstractions.Persistence;
using ERMS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ERMS.Infrastructure.Repositories;

/// <summary>
/// Basit CRUD işlemleri için tüm entity'lerde ortak generic repository implementasyonu.
/// Not: <see cref="GetAllAsync"/> ve <see cref="FindAsync"/> AsNoTracking ile okur (salt okunur listeleme).
/// Bir entity'yi güncellemek istiyorsan tracking açık olan <see cref="GetByIdAsync"/> ile çek,
/// aksi halde <see cref="Update"/> çağrısı EF'in yeniden attach ettiği entity'de dolu olmayan
/// alanları da "modified" işaretleyip üzerine yazabilir.
/// </summary>
public sealed class Repository<TEntity> : IRepository<TEntity>
    where TEntity : class
{
    private readonly ApplicationDbContext _dbContext;
    private readonly DbSet<TEntity> _dbSet;

    public Repository(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
        _dbSet = dbContext.Set<TEntity>();
    }

    public IQueryable<TEntity> Query()
    {
        return _dbSet.AsQueryable();
    }

    public async Task<TEntity?> GetByIdAsync(
        int id,
        CancellationToken cancellationToken = default)
    {
        return await _dbSet.FindAsync([id], cancellationToken);
    }

    public async Task<TEntity?> FirstOrDefaultAsync(
        Expression<Func<TEntity, bool>> predicate,
        CancellationToken cancellationToken = default)
    {
        return await _dbSet.FirstOrDefaultAsync(predicate, cancellationToken);
    }

    public async Task<List<TEntity>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    public async Task<List<TEntity>> FindAsync(
        Expression<Func<TEntity, bool>> predicate,
        CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .AsNoTracking()
            .Where(predicate)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(
        TEntity entity,
        CancellationToken cancellationToken = default)
    {
        await _dbSet.AddAsync(entity, cancellationToken);
    }

    public async Task AddRangeAsync(
        IEnumerable<TEntity> entities,
        CancellationToken cancellationToken = default)
    {
        await _dbSet.AddRangeAsync(entities, cancellationToken);
    }

    public void Update(TEntity entity)
    {
        _dbSet.Update(entity);
    }

    public void Remove(TEntity entity)
    {
        _dbSet.Remove(entity);
    }
}
