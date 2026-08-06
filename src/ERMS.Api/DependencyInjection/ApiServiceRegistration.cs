using ERMS.Api.Authentication;
using ERMS.Application.Abstractions.Authentication;
using ERMS.Application.Abstractions.Persistence;
using ERMS.Application.Interfaces;
using ERMS.Application.Services;
using ERMS.Application.Validators;
using ERMS.Infrastructure.Authentication;
using ERMS.Infrastructure.Persistence;
using ERMS.Infrastructure.Repositories;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace ERMS.Api.DependencyInjection;

/// <summary>
/// Composition root: DbContext, generic repository, UnitOfWork ve auth servislerinin
/// kayıtları burada toplanır. Application/Infrastructure katmanları kendi
/// DependencyInjection.cs dosyalarını barındırmaz — gerçek DI container burada
/// (Program.cs → WebApplication.CreateBuilder) oluşur.
/// </summary>
public static class ApiServiceRegistration
{
    public static IServiceCollection AddErmsServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        AddPersistence(services, configuration);
        AddAuthenticationServices(services, configuration);
        AddApplicationServices(services);

        return services;
    }

    private static void AddApplicationServices(IServiceCollection services)
    {
        services.AddScoped<IRequestService, RequestService>();

        services.AddValidatorsFromAssemblyContaining<CreateRequestDtoValidator>();
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

    private static void AddAuthenticationServices(
        IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<JwtSettings>(configuration.GetSection(JwtSettings.SectionName));

        services.AddScoped<IPasswordHasher, PasswordHasher>();
        services.AddScoped<IJwtTokenService, JwtTokenService>();
        services.AddScoped<IAuthService, AuthService>();

        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentUserService, CurrentUserService>();
    }
}
