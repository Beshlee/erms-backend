using ERMS.Domain.Enums;

namespace ERMS.Application.Abstractions.Authentication;

/// <summary>
/// İstek (HTTP request) bazında giriş yapmış kullanıcının kimliğine erişim.
/// Implementasyonu ERMS.Api'de (HttpContext'e bağımlı olduğu için) — Application
/// katmanı ASP.NET Core'un HttpContext'ini doğrudan bilmemeli.
/// </summary>
public interface ICurrentUserService
{
    /// <summary>Giriş yapılmamışsa null.</summary>
    int? UserId { get; }

    UserRole? Role { get; }

    bool IsAuthenticated { get; }
}
