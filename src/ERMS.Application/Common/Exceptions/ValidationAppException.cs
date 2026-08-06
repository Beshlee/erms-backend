namespace ERMS.Application.Common.Exceptions;

/// <summary>
/// 400 — girdi doğrulaması başarısız (FR-43, FR-45). Alan bazlı hata listesi taşır
/// (Bölüm 5.3 örneği: { "endDate": ["Bitiş tarihi başlangıçtan önce olamaz."] }).
/// FluentValidation entegrasyonu Gün 4'te bu istisnayı fırlatacak şekilde bağlanacak.
/// </summary>
public sealed class ValidationAppException : AppException
{
    public ValidationAppException(IReadOnlyDictionary<string, string[]> errors)
        : base(AppStatusCodes.Status400BadRequest, "VALIDATION_ERROR", "Doğrulama hatası.")
    {
        Errors = errors;
    }

    public IReadOnlyDictionary<string, string[]> Errors { get; }
}
