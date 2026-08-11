using ERMS.Application.Abstractions.Storage;
using Microsoft.Extensions.Configuration;

namespace ERMS.Infrastructure.Storage;

/// <summary>
/// <see cref="IFileStorageService"/>'in yerel diske yazan implementasyonu (bonus özellik —
/// gerçek bir üretim ortamında bunun yerine Azure Blob/S3 gibi bir servis kullanılırdı).
/// Kök klasör appsettings.json → "AttachmentStorage:RootPath" ile yapılandırılır.
/// </summary>
public sealed class LocalFileStorageService : IFileStorageService
{
    private readonly string _rootPath;

    public LocalFileStorageService(IConfiguration configuration)
    {
        var configuredPath = configuration["AttachmentStorage:RootPath"] ?? "App_Data/attachments";
        _rootPath = Path.IsPathRooted(configuredPath)
            ? configuredPath
            : Path.Combine(AppContext.BaseDirectory, configuredPath);
    }

    public async Task<string> SaveAsync(
        int requestId,
        string fileName,
        Stream content,
        CancellationToken cancellationToken = default)
    {
        var requestFolder = Path.Combine(_rootPath, requestId.ToString());
        Directory.CreateDirectory(requestFolder);

        // Orijinal dosya adı hem çakışabilir hem de path traversal riski taşır
        // ("../../..") — bu yüzden diskte her zaman rastgele/benzersiz bir ad kullanılır.
        // Kullanıcının gördüğü orijinal ad ayrıca RequestAttachment.FileName'de saklanır.
        var extension = Path.GetExtension(fileName);
        var storedFileName = $"{Guid.NewGuid():N}{extension}";
        var fullPath = Path.Combine(requestFolder, storedFileName);

        await using var fileStream = new FileStream(fullPath, FileMode.CreateNew, FileAccess.Write);
        await content.CopyToAsync(fileStream, cancellationToken);

        // DB'de göreli ve platformdan bağımsız (öne slash) bir anahtar saklanır.
        return $"{requestId}/{storedFileName}";
    }

    public Task<Stream> OpenReadAsync(string storageKey, CancellationToken cancellationToken = default)
    {
        var fullPath = Path.Combine(_rootPath, storageKey.Replace('/', Path.DirectorySeparatorChar));

        if (!File.Exists(fullPath))
        {
            throw new FileNotFoundException("Dosya diskte bulunamadı.", fullPath);
        }

        Stream stream = new FileStream(fullPath, FileMode.Open, FileAccess.Read);
        return Task.FromResult(stream);
    }
}
