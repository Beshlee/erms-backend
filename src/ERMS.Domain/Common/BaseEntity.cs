namespace ERMS.Domain.Common;

/// <summary>
/// Tüm entity'lerin (User, Request, Department, ...) miras aldığı temel sınıf — yalnızca
/// ortak birincil anahtarı (Id) taşır. Kod tekrarını önlemek için burada tanımlanmıştır;
/// "abstract" olması, doğrudan <c>new BaseEntity()</c> ile bir örneği oluşturulamayacağı,
/// yalnızca ondan türeyen somut sınıfların (User, Request vb.) kullanılabileceği anlamına gelir.
/// </summary>
public abstract class BaseEntity
{
    public int Id { get; set; }
}
