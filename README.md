# ERMS Backend (erms-backend)

Employee Request Management System — REST API (.NET 8 / ASP.NET Core).

Bu repo yalnızca backend'i içerir. Frontend, bir üst klasörde (`../erms-frontend`) duran
**tamamen ayrı bir git reposudur** — ikisi kardeş klasörler olarak aynı üst dizinde durur.
İki proje yalnızca REST API + JSON + JWT üzerinden haberleşir.

## Mimari

Katmanlı (clean-ish) mimari — bağımlılık yönü `Api → Application → Domain`, `Infrastructure` ise
`Application`'ın tanımladığı arayüzleri uygular:

```
src/
├── ERMS.Api/              # Controller'lar, DI composition root, Program.cs
├── ERMS.Application/      # Servisler, DTO'lar, repository arayüzleri (henüz iskelet)
├── ERMS.Domain/           # Entity'ler, enum'lar — dış bağımlılığı yok
└── ERMS.Infrastructure/   # EF Core DbContext, Configuration'lar, repository implementasyonları
tests/
└── ERMS.Tests/            # xUnit (iskelet)
```

## Teknoloji Yığını

- .NET 8 — ASP.NET Core Web API
- Entity Framework Core 8 (Code-First + Migrations) — SQL Server (LocalDB)
- Generic Repository (`IRepository<T>`) + karmaşık sorgular için `IRequestQueryRepository`
- JWT (`Microsoft.AspNetCore.Authentication.JwtBearer`) — kimlik doğrulama ve rol tabanlı yetkilendirme
- BCrypt (`BCrypt.Net-Next`) — parola hash'leme
- Global exception middleware — standart hata modeli (Bölüm 5.6)
- FluentValidation — girdi doğrulama (Bölüm 6.3)
- CORS — `erms-frontend` (varsayılan `http://localhost:4200`) `appsettings.json → AllowedOrigins`
- Swagger/OpenAPI (Swashbuckle, JWT "Authorize" desteğiyle)
- (Planlanan) AutoMapper

## Kurulum ve Çalıştırma

Gereksinimler: .NET 8 SDK, SQL Server LocalDB (Windows'ta genelde hazır gelir).

```bash
# Bağımlılıkları geri yükle ve derle
dotnet build ERMS.slnx

# Veritabanını oluştur / migration'ları uygula
dotnet ef database update --project src/ERMS.Infrastructure --startup-project src/ERMS.Api

# API'yi çalıştır
dotnet run --project src/ERMS.Api
```

Geliştirme ortamında migration'lar `dotnet run` sırasında otomatik uygulanır ve örnek veri
(seed) eklenir — ayrıca `dotnet ef database update` çalıştırmana gerek yok.

Swagger arayüzü geliştirme ortamında `https://localhost:<port>/swagger` adresinde açılır.
Sağ üstteki **Authorize** butonuna `Bearer <token>` yapıştırarak korumalı uç noktaları
Swagger üzerinden de deneyebilirsin.

Bağlantı dizesi `src/ERMS.Api/appsettings.json` içindeki `ConnectionStrings:DefaultConnection`
altında tanımlıdır (varsayılan: `(localdb)\mssqllocaldb`, veritabanı adı `ErmsDb`).

### Test kullanıcıları (seed data)

Her birinin parolası **`Passw0rd!`**:

| E-posta | Rol |
|---|---|
| `ahmet@sirket.com` | Employee |
| `mehmet@sirket.com` | Manager (Ahmet'in yöneticisi) |
| `admin@erms.com` | Admin |

⚠️ Bu bilgiler yalnızca yerel geliştirme/demo amaçlıdır — gerçek bir ortamda asla kullanılmamalı.
Aynı şekilde `appsettings.json`'daki JWT `Secret` değeri de yalnızca geliştirme içindir;
gerçek bir deployment'ta ortam değişkeni / user-secrets'a taşınmalı, asla kaynak koduna
gömülü bırakılmamalıdır.

## Proje Durumu

Kaynak doküman: [`ERMS_Stajyer_Analiz_Dokumani.docx`](../ERMS_Stajyer_Analiz_Dokumani.docx) (üst klasörde,
her iki repo için de ortak referans) — 48 fonksiyonel gereksinim (FR-01..FR-48), User Story'ler,
ER diyagramı, REST API sözleşmeleri ve sprint planını içerir.

- [x] Katmanlı proje iskeleti + EF Core bağlantısı (Gün 1)
- [x] Domain entity'leri + DbContext + ilişkiler + ilk migration (Gün 2)
- [x] JWT login + rol tabanlı yetkilendirme (Gün 3, FR-01..06)
- [x] Talep oluşturma + taslak + doğrulama (Gün 4, FR-16..22)
- [x] Talep listeleme + filtre + arama + sayfalama (Gün 5, FR-25..29)
- [x] Gönderme/iptal + durum geçişleri + audit log (Gün 6, FR-23,30,41,42)
- [x] Onay akışı (Gün 7, FR-32..37)
- [x] Yorumlar + admin kullanıcı/departman/talep türü yönetimi (Gün 8, FR-07..15, FR-38, FR-39)
- [ ] Global hata yönetimi, Swagger temizliği, README (Gün 10)

Not: `UserResponseDto`'ya sonradan `DepartmentId` eklendi (frontend'in admin kullanıcı düzenleme
formunda departman dropdown'ını isim eşleştirmesi gibi kırılgan bir yöntemle değil, doğrudan ID
ile önceden doldurabilmesi için — bkz. `erms-frontend` Ekran 6 çalışması).
