using ERMS.Domain.Entities;
using ERMS.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace ERMS.Infrastructure.Persistence.Seed;

/// <summary>
/// Geliştirme ortamında giriş yapıp API'yi test edebilmek için örnek veri
/// (Bölüm 1.5 — Detaylı Senaryo'daki Ahmet/Mehmet/Ayşe karakterleri temel alınmıştır).
/// Idempotent: Users tablosu boş değilse hiçbir şey yapmaz.
/// </summary>
public static class DatabaseSeeder
{
    /// <summary>Tüm seed kullanıcılarının şifresi (yalnızca geliştirme/demo amaçlı).</summary>
    public const string DefaultPassword = "Passw0rd!";

    public static async Task SeedAsync(ApplicationDbContext dbContext, CancellationToken cancellationToken = default)
    {
        if (await dbContext.Users.AnyAsync(cancellationToken))
        {
            return;
        }

        var department = new Department { Name = "Yazılım", IsActive = true, CreatedAt = DateTime.UtcNow };
        await dbContext.Departments.AddAsync(department, cancellationToken);

        var requestTypes = new List<RequestType>
        {
            new() { Name = "İzin", RequiresApproval = true, IsActive = true },
            new() { Name = "Masraf", RequiresApproval = true, IsActive = true },
            new() { Name = "Donanım", RequiresApproval = true, IsActive = true },
            new() { Name = "Genel", RequiresApproval = false, IsActive = true }
        };
        await dbContext.RequestTypes.AddRangeAsync(requestTypes, cancellationToken);

        var passwordHash = BCrypt.Net.BCrypt.HashPassword(DefaultPassword);

        var manager = new User
        {
            FirstName = "Mehmet",
            LastName = "Demir",
            Email = "mehmet@sirket.com",
            PasswordHash = passwordHash,
            Role = UserRole.Manager,
            Department = department,
            IsActive = true
        };
        await dbContext.Users.AddAsync(manager, cancellationToken);

        var employee = new User
        {
            FirstName = "Ahmet",
            LastName = "Yılmaz",
            Email = "ahmet@sirket.com",
            PasswordHash = passwordHash,
            Role = UserRole.Employee,
            Department = department,
            Manager = manager,
            IsActive = true
        };
        await dbContext.Users.AddAsync(employee, cancellationToken);

        var admin = new User
        {
            FirstName = "Ayşe",
            LastName = "Kaya",
            Email = "admin@erms.com",
            PasswordHash = passwordHash,
            Role = UserRole.Admin,
            Department = department,
            IsActive = true
        };
        await dbContext.Users.AddAsync(admin, cancellationToken);

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
