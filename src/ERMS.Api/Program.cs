using System.Text;
using System.Text.Json.Serialization;
using System.Threading.RateLimiting;
using ERMS.Api.DependencyInjection;
using ERMS.Api.Middleware;
using ERMS.Infrastructure.Authentication;
using ERMS.Infrastructure.Persistence;
using ERMS.Infrastructure.Persistence.Seed;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers()
    // Enum'lar JSON'da sayı yerine isim olarak okunsun/yazılsın (ör. "priority": "Normal"),
    // dokümandaki (Bölüm 5.3) örnek istek/yanıt gövdeleriyle birebir uyumlu olması için.
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });

// [ApiController], nullable olmayan string alanları (ör. Title, Comment) JSON'dan tamamen
// eksik gelirse otomatik olarak "zorunlu" sayıp kendi ProblemDetails formatıyla 400 döner —
// bu, controller'a hiç girmeden FluentValidation'ı (ve standart hata modelimizi, Bölüm 5.6)
// devre dışı bırakır. Bunu kapatıyoruz ki TÜM doğrulama hataları tek, tutarlı bir formattan
// (ValidationAppException → ExceptionHandlingMiddleware) geçsin.
builder.Services.Configure<Microsoft.AspNetCore.Mvc.ApiBehaviorOptions>(options =>
{
    options.SuppressModelStateInvalidFilter = true;
});

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    // Swagger UI'da "Authorize" butonu ile JWT girilebilmesi için (FR-48).
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "JWT token'i 'Bearer {token}' formatında girin."
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
            },
            Array.Empty<string>()
        }
    });

    // FR-48: controller/action'lardaki /// <summary> yorumları Swagger UI'da endpoint
    // açıklaması olarak görünsün (ERMS.Api.csproj → GenerateDocumentationFile).
    var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
    {
        options.IncludeXmlComments(xmlPath);
    }
});

builder.Services.AddErmsServices(builder.Configuration);

var jwtSettings = builder.Configuration.GetSection(JwtSettings.SectionName).Get<JwtSettings>()
    ?? throw new InvalidOperationException("Jwt yapılandırması (appsettings.json → \"Jwt\") eksik.");

// Secret artık appsettings.json'da DEĞİL (review bulgusu — git geçmişine gömülü bir sır asla
// güvenli sayılmaz). Yerelde "dotnet user-secrets set Jwt:Secret <değer>" ile, container'da
// JWT_SECRET ortam değişkeniyle sağlanmalı (bkz. README → Kurulum, docker-compose.yml).
if (string.IsNullOrWhiteSpace(jwtSettings.Secret))
{
    throw new InvalidOperationException(
        "Jwt:Secret tanımlı değil. Yerel geliştirmede: "
        + "'dotnet user-secrets set \"Jwt:Secret\" \"<en az 32 karakterlik rastgele bir değer>\" "
        + "--project src/ERMS.Api'. Docker'da: JWT_SECRET ortam değişkeni / .env dosyası "
        + "(bkz. .env.example).");
}

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtSettings.Issuer,
            ValidAudience = jwtSettings.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.Secret)),
            ClockSkew = TimeSpan.Zero
        };

        // FR-04 / FR-06: token eksik/geçersizse ve rol yetersizse de standart hata modeli dönsün
        // (aksi halde ASP.NET Core varsayılan olarak boş gövdeli 401/403 döner).
        options.Events = new JwtBearerEvents
        {
            OnChallenge = async context =>
            {
                context.HandleResponse();
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                context.Response.ContentType = "application/json";
                await context.Response.WriteAsync(
                    """{"code":"UNAUTHORIZED","message":"Geçerli bir token gerekli."}""");
            },
            OnForbidden = async context =>
            {
                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                context.Response.ContentType = "application/json";
                await context.Response.WriteAsync(
                    """{"code":"FORBIDDEN","message":"Bu işlem için yetkiniz yok."}""");
            }
        };
    });

builder.Services.AddAuthorization();

// erms-frontend (Angular, ayrı repo/origin) API'yi tüketebilsin diye CORS.
// İzinli origin'ler appsettings.json → "AllowedOrigins" içinden okunur.
builder.Services.AddCors(options =>
{
    options.AddPolicy("AngularClient", policy =>
    {
        var origins = builder.Configuration.GetSection("AllowedOrigins").Get<string[]>()
            ?? ["http://localhost:4200"];

        policy
            .WithOrigins(origins)
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

// Review bulgusu: /api/auth/login için IP başına hız sınırı — otomatik parola deneme
// (brute-force) saldırılarını yavaşlatır. FR'lerde istenmiyor ama Bölüm 8.2'deki
// "Doğrulama ve güvenlik" değerlendirme kriterinin kapsamına giren makul bir sertleştirme.
// Normal bir kullanıcının 1 dakikada 5'ten fazla giriş denemesi olması beklenmez.
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    options.AddPolicy("auth", httpContext => RateLimitPartition.GetFixedWindowLimiter(
        partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
        factory: _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = 5,
            Window = TimeSpan.FromMinutes(1),
            QueueLimit = 0
        }));
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();

    // Geliştirmede migration'ları otomatik uygula ve örnek veriyi ekle
    // (manuel "dotnet ef database update" gerekmesin diye).
    using var scope = app.Services.CreateScope();
    var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    await dbContext.Database.MigrateAsync();
    await DatabaseSeeder.SeedAsync(dbContext);
}

app.UseMiddleware<ExceptionHandlingMiddleware>();

app.UseCors("AngularClient");

app.UseRateLimiter();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
