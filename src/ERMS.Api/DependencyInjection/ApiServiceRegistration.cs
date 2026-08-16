using ERMS.Api.Authentication;
using ERMS.Application.Abstractions.Authentication;
using ERMS.Application.Abstractions.Persistence;
using ERMS.Application.Abstractions.Storage;
using ERMS.Application.Interfaces;
using ERMS.Application.Services;
using ERMS.Application.Validators;
using ERMS.Infrastructure.Authentication;
using ERMS.Infrastructure.Persistence;
using ERMS.Infrastructure.Repositories;
using ERMS.Infrastructure.Storage;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace ERMS.Api.DependencyInjection;

/// <summary>
/// Composition root: DbContext, generic repository, UnitOfWork ve auth servislerinin
/// kayıtları burada toplanır. Application/Infrastructure katmanları kendi
/// DependencyInjection.cs dosyalarını barındırmaz — gerçek DI container burada
/// (Program.cs → WebApplication.CreateBuilder) oluşur.
///
/// Burada asıl olan şey Dependency Inversion Principle'ın "somutlaştığı" yer olması:
/// Application katmanı yalnızca arayüzleri (ör. IRequestService, IUnitOfWork) bilir, kim
/// bunları implemente ediyor umursamaz. Bu dosya, "IX isteneninde Y ver" diyerek arayüzleri
/// somut sınıflara bağlar (ör. <c>services.AddScoped&lt;IRequestService, RequestService&gt;()</c>).
/// <c>AddScoped</c>, o servisin ömrünü belirler: HER HTTP isteği için YENİ bir örnek
/// oluşturulur, aynı istek içindeki tüm bağımlılıklar aynı örneği paylaşır (ör. bir
/// controller action'ı içinde çağrılan iki servis aynı DbContext'i kullanır — bu da EF
/// Core'un "change tracking"inin doğru çalışması için önemlidir). Alternatifleri
/// <c>AddSingleton</c> (uygulama boyunca tek örnek) ve <c>AddTransient</c>'tir
/// (her enjeksiyonda yeni örnek) — bu projede DbContext'e bağımlı olan hemen her şey
/// Scoped olmak zorundadır.
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
        services.AddScoped<IApprovalService, ApprovalService>();
        services.AddScoped<ICommentService, CommentService>();
        services.AddScoped<IUserService, UserService>();
        services.AddScoped<IDepartmentService, DepartmentService>();
        services.AddScoped<IRequestTypeService, RequestTypeService>();
        services.AddScoped<IAttachmentService, AttachmentService>();
        services.AddScoped<IFileStorageService, LocalFileStorageService>();
        services.AddScoped<IReportService, ReportService>();

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

        // Admin rapor ekranı (bonus) için GroupBy/Count sorguları.
        services.AddScoped<IReportQueryRepository, ReportQueryRepository>();

        services.AddScoped<IUnitOfWork, UnitOfWork>();
    }

    private static void AddAuthenticationServices(
        IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<JwtSettings>(configuration.GetSection(JwtSettings.SectionName));

        services.AddScoped<IPasswordHasher, PasswordHasher>();
        services.AddScoped<IJwtTokenService, JwtTokenService>();
        // Durumsuz/saf bir fonksiyon (SHA-256) olduğu için Singleton — bkz. IRefreshTokenHasher.
        services.AddSingleton<IRefreshTokenHasher, RefreshTokenHasher>();
        services.AddScoped<IAuthService, AuthService>();

        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentUserService, CurrentUserService>();
    }
}
