using ERMS.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace ERMS.Infrastructure.Persistence;

/// <summary>
/// EF Core'un veritabanıyla konuştuğu tek nokta — her <c>DbSet&lt;T&gt;</c> özelliği bir
/// SQL Server tablosuna karşılık gelir (ör. <see cref="Users"/> → "Users" tablosu). Bu sınıf
/// üzerinden LINQ sorguları (ör. <c>_dbContext.Users.Where(...)</c>) EF Core tarafından SQL'e
/// çevrilir. Generic Repository (<see cref="Repositories.Repository{TEntity}"/>) ve
/// RequestQueryRepository gibi sorgu sınıfları, bu sınıfı sarmalayarak servis katmanının
/// EF Core'a doğrudan bağımlı olmasını önler.
/// </summary>
public sealed class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<Department> Departments => Set<Department>();
    public DbSet<RequestType> RequestTypes => Set<RequestType>();
    public DbSet<Request> Requests => Set<Request>();
    public DbSet<Approval> Approvals => Set<Approval>();
    public DbSet<RequestComment> RequestComments => Set<RequestComment>();
    public DbSet<RequestAttachment> RequestAttachments => Set<RequestAttachment>();
    public DbSet<RequestHistory> RequestHistory => Set<RequestHistory>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Her entity'nin (User, Request, ...) kendi Configurations/*.cs dosyasındaki
        // kuralları (max uzunluk, ilişkiler, unique index vb.) burada TEK TEK yazmak
        // yerine, o klasördeki tüm IEntityTypeConfiguration<T> sınıflarını otomatik bulup
        // uygular — yeni bir *Configuration.cs eklemek bu metodu değiştirmeyi gerektirmez.
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);

        base.OnModelCreating(modelBuilder);
    }
}
