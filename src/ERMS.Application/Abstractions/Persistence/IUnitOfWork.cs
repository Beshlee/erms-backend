namespace ERMS.Application.Abstractions.Persistence;

/// <summary>
/// "Unit of Work" deseni: bir servis metodu içinde birden çok repository üzerinde yapılan
/// değişiklikleri (ör. hem Request'i güncelle hem RequestHistory ekle) TEK bir veritabanı
/// işleminde (transaction) kalıcı hale getirmeyi sağlar. <see cref="IRepository{TEntity}"/>
/// metotları (Update, Remove, AddAsync) değişiklikleri yalnızca EF Core'un hafızadaki
/// "change tracker"ına işaretler; asıl SQL ancak <see cref="SaveChangesAsync"/> çağrıldığında
/// çalışır. Bu sayede bir servis metodu yarı tamamlanmış (ör. Request güncellendi ama
/// RequestHistory eklenemedi) bir durumda veritabanına yazmaz.
/// </summary>
public interface IUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
