namespace ERMS.Application.Abstractions.Storage;

/// <summary>
/// Fiziksel dosya depolama soyutlaması (FR-40 bonus). Infrastructure bunu yerel diske
/// yazarak uygular; ileride bulut depolamaya (örn. Azure Blob) geçilirse yalnızca
/// Infrastructure'daki implementasyon değişir, Application/Api etkilenmez.
/// </summary>
public interface IFileStorageService
{
    /// <summary>
    /// İçeriği kaydeder ve daha sonra <see cref="OpenReadAsync"/> ile okumak için
    /// kullanılacak saklama anahtarını (göreli yol) döner.
    /// </summary>
    Task<string> SaveAsync(
        int requestId,
        string fileName,
        Stream content,
        CancellationToken cancellationToken = default);

    /// <summary><see cref="SaveAsync"/>'in döndürdüğü anahtarla dosyayı okumak için bir akış açar.</summary>
    Task<Stream> OpenReadAsync(
        string storageKey,
        CancellationToken cancellationToken = default);
}
