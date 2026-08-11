namespace ERMS.Domain.Enums;

/// <summary>
/// Talebin öncelik derecesi — yalnızca bilgi amaçlıdır, iş kurallarını (onay akışı, durum
/// geçişleri) etkilemez; listeleme/filtreleme ve görsel önem vurgusu için kullanılır.
/// </summary>
public enum RequestPriority
{
    Low = 0,
    Normal = 1,
    High = 2
}
