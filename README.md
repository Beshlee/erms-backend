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
- Swagger/OpenAPI (Swashbuckle)
- (Planlanan) JWT, FluentValidation, AutoMapper, BCrypt

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

Swagger arayüzü geliştirme ortamında `https://localhost:<port>/swagger` adresinde açılır.

Bağlantı dizesi `src/ERMS.Api/appsettings.json` içindeki `ConnectionStrings:DefaultConnection`
altında tanımlıdır (varsayılan: `(localdb)\mssqllocaldb`, veritabanı adı `ErmsDb`).

## Proje Durumu

Kaynak doküman: [`ERMS_Stajyer_Analiz_Dokumani.docx`](../ERMS_Stajyer_Analiz_Dokumani.docx) (üst klasörde,
her iki repo için de ortak referans) — 48 fonksiyonel gereksinim (FR-01..FR-48), User Story'ler,
ER diyagramı, REST API sözleşmeleri ve sprint planını içerir.

- [x] Katmanlı proje iskeleti + EF Core bağlantısı (Gün 1)
- [x] Domain entity'leri + DbContext + ilişkiler + ilk migration (Gün 2)
- [ ] JWT login + rol tabanlı yetkilendirme (Gün 3, FR-01..06)
- [ ] Talep oluşturma + taslak + doğrulama (Gün 4, FR-16..22)
- [ ] Talep listeleme + filtre + arama + sayfalama (Gün 5, FR-25..29)
- [ ] Gönderme/iptal + durum geçişleri + audit log (Gün 6, FR-23,30,41)
- [ ] Onay akışı (Gün 7, FR-32..37)
- [ ] Yorumlar + admin tür yönetimi (Gün 8, FR-13,38,39)
- [ ] Global hata yönetimi, Swagger temizliği, README (Gün 10)
