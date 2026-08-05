namespace ERMS.Domain.Common;

/// <summary>
/// Tüm entity'ler için ortak birincil anahtar tanımı.
/// </summary>
public abstract class BaseEntity
{
    public int Id { get; set; }
}
