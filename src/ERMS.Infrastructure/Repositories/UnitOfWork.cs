using ERMS.Application.Abstractions.Persistence;
using ERMS.Infrastructure.Persistence;

namespace ERMS.Infrastructure.Repositories;

/// <summary>
/// IUnitOfWork'ün EF Core implementasyonu — açıklaması (neden var, ne işe yarar) arayüzde
/// (<see cref="IUnitOfWork"/>). Burada yapılan tek şey <see cref="ApplicationDbContext"/>'in
/// zaten sahip olduğu SaveChangesAsync'i olduğu gibi dışarı açmak; bunu ayrı bir arayüz
/// arkasına koymamızın sebebi, Application katmanının doğrudan EF Core'a (DbContext'e)
/// değil, kendi tanımladığı soyut bir sözleşmeye bağımlı olması (Dependency Inversion).
/// </summary>
public sealed class UnitOfWork : IUnitOfWork
{
    private readonly ApplicationDbContext _dbContext;

    public UnitOfWork(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return _dbContext.SaveChangesAsync(cancellationToken);
    }
}
