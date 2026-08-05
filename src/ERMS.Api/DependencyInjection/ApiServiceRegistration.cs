using ERMS.Application.Abstractions.Persistence;
using ERMS.Infrastructure.Persistence;
using ERMS.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;

namespace ERMS.Api.DependencyInjection;

/// <summary>
/// Composition root: DbContext, generic repository ve UnitOfWork kayıtları burada toplanır.
/// Application/Infrastructure katmanları kendi DependencyInjection.cs dosyalarını barındırmaz —
/// gerçek DI container burada (Program.cs → WebApplication.CreateBuilder) oluşur.
/// </summary>
public static class ApiServiceRegistration
{
    public static IServiceCollection AddErmsServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        AddPersistence(services, configuration);

        return services;
    }

    private static void AddPersistence(
        IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddDbContext<ApplicationDbContext>(options =>
        {
            options.UseSqlServer(
                configuration.GetConnectionString("DefaultConnection"));
        });

        // Generic repository — herhangi bir TEntity için basit CRUD.
        services.AddScoped(typeof(IRepository<>), typeof(Repository<>));

        // Include/filtre/sayfalama gerektiren karmaşık Request sorguları için özel query repository.
        services.AddScoped<IRequestQueryRepository, RequestQueryRepository>();

        services.AddScoped<IUnitOfWork, UnitOfWork>();
    }
}
