# Bonus (Bölüm 8.3) — "docker-compose ile ayağa kaldırma".
# Build context repo kökü (erms-backend/) olmalı — bkz. docker-compose.yml
# ("context: .", "dockerfile: src/ERMS.Api/Dockerfile").

# --- 1) Derleme/publish aşaması ---
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Önce yalnızca .csproj dosyalarını kopyalayıp restore ediyoruz — Docker layer cache'i
# sayesinde yalnızca .cs dosyaları değiştiğinde (paketler aynı kaldığı sürece) "dotnet
# restore" tekrar çalışmaz, bu da yeniden derlemeyi belirgin şekilde hızlandırır.
COPY src/ERMS.Domain/ERMS.Domain.csproj src/ERMS.Domain/
COPY src/ERMS.Application/ERMS.Application.csproj src/ERMS.Application/
COPY src/ERMS.Infrastructure/ERMS.Infrastructure.csproj src/ERMS.Infrastructure/
COPY src/ERMS.Api/ERMS.Api.csproj src/ERMS.Api/
RUN dotnet restore src/ERMS.Api/ERMS.Api.csproj

# Test projesi (tests/ERMS.Tests) bilinçli olarak kopyalanmıyor — imaja gerekli değil.
COPY src/ src/
RUN dotnet publish src/ERMS.Api/ERMS.Api.csproj -c Release -o /app/publish --no-restore

# --- 2) Çalışma zamanı aşaması ---
# SDK yerine yalnızca ASP.NET Core runtime içeren küçük imaj — SDK ~800MB, bu ~220MB.
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "ERMS.Api.dll"]
