namespace ERMS.Infrastructure.Authentication;

/// <summary>appsettings.json → "Jwt" bölümüne karşılık gelir.</summary>
public sealed class JwtSettings
{
    public const string SectionName = "Jwt";

    public string Secret { get; set; } = null!;
    public string Issuer { get; set; } = null!;
    public string Audience { get; set; } = null!;
    public int ExpiryMinutes { get; set; } = 60;
}
