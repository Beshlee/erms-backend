# ERMS Backend (erms-backend)

Employee Request Management System — REST API (.NET 8 / ASP.NET Core).

Bu repo yalnızca backend'i içerir. Frontend, bir üst klasörde (`../erms-frontend`) duran
**tamamen ayrı bir git reposudur** — ikisi kardeş klasörler olarak aynı üst dizinde durur.
İki proje yalnızca REST API + JSON + JWT üzerinden haberleşir.

## Proje hakkında önemli not

Bu proje bir **staj/öğrenme çalışmasıdır**, üretim (production) ortamında kullanılmak üzere
tasarlanmamıştır. Geliştirme süreci, projenin sahibi olan stajyerin **Claude (Anthropic)
yapay zeka asistanıyla birlikte** adım adım ilerlediği bir çalışmadır: her katman/özellik
önce birlikte tasarlanıp sonra gerçek bir ortamda (LocalDB, tarayıcı, curl) test edilerek
doğrulanmış; alınan kararların gerekçeleri (neden bu desen, hangi sınırlama bilinçli kabul
edildi) kod içi yorumlarla ve bu README'de belgelenmeye çalışılmıştır. Amaç kusursuz bir ürün
değil, .NET ile katmanlı bir mimariyi uçtan uca kurup çalıştırma sürecini anlamaktır.

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
- [x] Global hata yönetimi, Swagger temizliği, README (Gün 10)
- [x] **Bonus:** Service katmanı için xUnit birim testleri (Moq ile mock'lanmış)
- [x] **Bonus:** Dosya eki yükleme/indirme (FR-40)
- [x] **Bonus:** Admin rapor ekranı (dashboard)
- [x] **Bonus:** Docker (docker-compose ile ayağa kaldırma)
- [x] **Bonus:** Global arama ve gelişmiş filtreleme
- [x] **Bonus:** Refresh token akışı

### Bonus — Docker

```bash
# Repo kökünden:
docker compose up --build
```

Bu tek komut iki container ayağa kaldırır:
- `sqlserver` — SQL Server 2022, verisi `erms-sqlserver-data` volume'ünde kalıcı.
- `api` — bu repodaki `src/ERMS.Api/Dockerfile` ile derlenir; `sqlserver` "healthy" olana kadar
  bekler (`depends_on: condition: service_healthy`), sonra `ASPNETCORE_ENVIRONMENT=Development`
  sayesinde Program.cs migration'ları otomatik uygular ve örnek veriyi (seed) ekler — elle
  `dotnet ef database update` gerekmez.

API `http://localhost:8080` üzerinden erişilebilir olur (Swagger: `http://localhost:8080/swagger`).
Bağlantı dizesi, JWT secret'ı ve CORS izinli origin'i (appsettings.json yerine) ortam
değişkenleriyle geçiliyor — kendi değerlerini kullanmak istersen `.env.example`'ı `.env` olarak
kopyalayıp doldurabilirsin (`.env` `.gitignore`'da, commit'lenmez).

`erms-frontend`'i ayrıca (host'ta, container dışında) `npm start` ile çalıştırıp
`environment.ts`'teki `apiUrl`'i `http://localhost:8080/api` yapman yeterli.

✅ **Gerçek `docker compose up --build` ile uçtan uca doğrulandı:** `erms-sqlserver` healthy
duruma geçti, `erms-api` migration'ları otomatik uyguladı + seed verisini ekledi ve `8080`
portunda ayağa kalktı; `POST http://localhost:8080/api/auth/login` gerçek bir JWT token
döndürdü, Swagger UI (`http://localhost:8080/swagger`) 200 döndü. (Docker bu makineye bu
bonus çalışması sırasında kuruldu — SQL Server healthcheck'indeki `mssql-tools18` yolu
doğru çıktı, ek bir düzeltme gerekmedi.)

### Bonus — Global arama ve gelişmiş filtreleme

`GET /api/requests` artık şunları da destekliyor:
- `search` — artık yalnızca başlıkta değil, **açıklamada ve talep türü adında** da arar
  ("global arama").
- `priority` (`Low`/`Normal`/`High`), `createdFrom`/`createdTo` (oluşturulma tarihi aralığı,
  `createdTo` günün sonuna kadar dahil), `minAmount`/`maxAmount` (tutar aralığı) — hepsi birlikte
  kullanılabilir ("gelişmiş filtreleme, birden çok kritere göre").
- Geçersiz `priority` değeri veya mantıksız aralıklar (`createdTo < createdFrom`,
  `maxAmount < minAmount`) 400 `VALIDATION_ERROR` döner — `status` filtresiyle aynı doğrulama
  yaklaşımı.

Gerçek çalışan instance'a karşı curl ile test edildi: açıklamada geçen ama başlıkta geçmeyen bir
kelimeyle arama, öncelik filtresi, tutar aralığı, tarih aralığı, birden çok filtrenin birlikte
kullanımı ve iki geçersiz-girdi senaryosu (400 dönmesi gereken).

### Bonus — Refresh token akışı

Access token (JWT) süresi kısa tutulabilir (`Jwt:ExpiryMinutes`, varsayılan 60 dk) çünkü artık
süresi dolduğunda kullanıcı dışarı atılmıyor — `login` yanıtındaki `refreshToken` ile sessizce
yenilenebiliyor.

- `POST /api/auth/login` yanıtına `refreshToken` eklendi (`Jwt:RefreshTokenExpiryDays`,
  varsayılan 7 gün geçerli).
- `POST /api/auth/refresh` — geçerli bir refresh token'ı yeni bir access+refresh token çiftine
  çevirir. **Rotation**: her kullanımda hem access hem refresh token yenilenir, eskisi hemen
  geçersiz olur — çalınmış bir refresh token'ın sınırsız kullanılmasını engeller.
- `POST /api/auth/logout` — refresh token'ı sunucu tarafında geçersiz kılar (bilinmeyen bir
  token için sessizce başarılı sayılır).
- **Bilinçli sınırlama:** kullanıcı başına yalnızca TEK aktif refresh token saklanıyor
  (`User.RefreshToken`/`RefreshTokenExpiresAt`) — yeni bir giriş ya da yenileme bir öncekini
  geçersiz kılar. Çoklu cihaz/oturum desteği (kullanıcı başına birden çok refresh token) bu
  kapsamın dışında bırakıldı; gerçek bir üretim sisteminde ayrı bir `RefreshToken` tablosu
  gerekirdi.

Gerçek çalışan instance'a karşı curl ile uçtan uca test edildi: login → refresh (yeni token
çifti döner) → **eski** refresh token'la tekrar refresh denemesi (401, rotation doğrulandı) →
geçersiz/rastgele token (401) → logout (204) → logout sonrası aynı token'la refresh denemesi
(401) → bilinmeyen token'la logout (sessizce 204).

Not: `UserResponseDto`'ya sonradan `DepartmentId` eklendi (frontend'in admin kullanıcı düzenleme
formunda departman dropdown'ını isim eşleştirmesi gibi kırılgan bir yöntemle değil, doğrudan ID
ile önceden doldurabilmesi için — bkz. `erms-frontend` Ekran 6 çalışması).

### Gün 10 — son temizlik notları
- `ERMS.Api.http` artık gerçek uç nokta örnekleriyle dolu (eski şablon `weatherforecast`
  isteği kaldırıldı).
- Swagger UI artık controller/action'lardaki `/// <summary>` XML yorumlarını gösteriyor
  (`GenerateDocumentationFile` + `IncludeXmlComments`, FR-48).
- Frontend'deki "Admin varsayılan yönlendirme" sınırlaması (önceki not) düzeltildi —
  bkz. `erms-frontend/README.md`.

### Bonus — Birim testleri (xUnit)

`tests/ERMS.Tests` altında, Application katmanındaki servislerin (AuthService, RequestService,
ApprovalService) iş kurallarını doğrulayan 19 test var. Repository/UnitOfWork/CurrentUserService
gibi bağımlılıklar **Moq** ile mock'lanıyor; FluentValidation validator'ları ise gerçek
örnekleriyle kullanılıyor (böylece testler gerçek doğrulama mantığını da kapsıyor).

Kapsanan başlıca kurallar:
- FR-01/06 — geçersiz parola ve pasif kullanıcı **aynı** hata mesajıyla reddedilir (bilgi sızıntısı yok)
- FR-18/19/20 — İzin türü için tarih, Masraf türü için tutar zorunluluğu; bitiş tarihi başlangıçtan önce olamaz
- FR-21 — taslak kayıtlarda tür bazlı zorunluluklar uygulanmaz
- FR-22 — yalnızca talep sahibi kendi taslağını düzenleyebilir; taslak dışı durumlar düzenlenemez
- FR-30 — yalnızca beklemede olan talepler iptal edilebilir
- FR-32/36/37 — yönetici yalnızca kendisine bağlı personelin talebini karara bağlayabilir, kendi
  talebini onaylayamaz, zaten sonuçlanmış bir talep tekrar karara bağlanamaz
- FR-34 — reddetme gerekçesi zorunludur

```bash
dotnet test tests/ERMS.Tests/ERMS.Tests.csproj
```

### Bonus — Dosya eki yükleme/indirme (FR-40)

`RequestAttachment` tablosu (altyapısı FR-40 çekirdek gerekliliğiyle zaten vardı) artık gerçekten
kullanılıyor. `IFileStorageService` soyutlaması Application'da tanımlı, Infrastructure'daki
`LocalFileStorageService` dosyaları yerel diske yazıyor (`appsettings.json → AttachmentStorage:RootPath`,
varsayılan `App_Data/attachments` — repoya girmez, `.gitignore`'da).

- `POST /api/requests/{id}/attachments` (multipart/form-data) — yalnızca **talep sahibi**,
  talep **Draft/Pending** durumundayken yükleyebilir (FR-22'deki düzenleme penceresiyle tutarlı).
- `GET /api/requests/{id}/attachments/{attachmentId}/download` — talep sahibi ve ilgili
  yönetici indirebilir (FR-26 ile aynı görünürlük kuralı).
- Doğrulama: en fazla **5 MB**, izinli uzantılar `.pdf, .jpg, .jpeg, .png, .doc, .docx, .xls, .xlsx`
  (aksi halde 400 `VALIDATION_ERROR`). Diskte her dosya rastgele bir adla saklanır (path traversal
  ve ad çakışması önlemi); orijinal ad yalnızca veritabanında (`FileName`) tutulur.
- `GET /api/requests/{id}` yanıtındaki `attachments` alanı, o talebe eklenmiş dosyaları listeler.

Tüm senaryolar (izinli/yasaklı tür, sahip/yönetici/ilgisiz kullanıcı, onaylanmış talebe ekleme
denemesi) gerçek bir çalışan instance'a karşı curl ile uçtan uca test edildi.

### Bonus — Admin rapor ekranı (dashboard)

`GET /api/admin/reports/summary` (yalnızca Admin) — toplam talep sayısı + durum/tür/departman
kırılımları. `IReportQueryRepository` (Infrastructure), `RequestQueryRepository` ile aynı
gerekçeyle GroupBy/Count sorgularını generic Repository'nin dışında topluyor; departman kırılımı
`Request → Requester → Department` iki seviyeli join gerektiriyor, EF Core bunu tek sorguda
SQL join+group by'a çeviriyor (gerçek DB'ye karşı doğrulandı). Durum kırılımında hiç talebi
olmayan bir durum bile (örn. `Cancelled: 0`) dashboard'da tutarlı görünsün diye 0 ile listelenir.
