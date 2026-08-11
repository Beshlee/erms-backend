using ERMS.Application.Abstractions.Authentication;
using ERMS.Application.Abstractions.Persistence;
using ERMS.Application.Abstractions.Storage;
using ERMS.Application.Common.Exceptions;
using ERMS.Application.DTOs.Requests;
using ERMS.Application.Interfaces;
using ERMS.Domain.Entities;
using ERMS.Domain.Enums;

namespace ERMS.Application.Services;

/// <summary>
/// Bonus (FR-40) — talebe dosya eki yükleme/indirme. Gerçek disk I/O'sunu bilmez, bunun
/// yerine <see cref="Abstractions.Storage.IFileStorageService"/> soyutlamasını kullanır —
/// böylece "dosya nereye/nasıl yazılır" kararı Infrastructure katmanında kalır.
/// </summary>
public sealed class AttachmentService : IAttachmentService
{
    // FR-40: "dosya boyutu/tip doğrulaması" — kötü amaçlı/aşırı büyük dosya yüklemeyi
    // sınırlamak için yaygın belge/görsel uzantılarıyla ve 5 MB'la sınırlıyoruz.
    // Doküman kesin bir liste/limit vermiyor; bu makul bir varsayılan.
    private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".pdf", ".jpg", ".jpeg", ".png", ".doc", ".docx", ".xls", ".xlsx"
    };

    private const long MaxFileSizeBytes = 5 * 1024 * 1024;

    private readonly IRepository<Request> _requestRepository;
    private readonly IRepository<RequestAttachment> _attachmentRepository;
    private readonly IRepository<User> _userRepository;
    private readonly ICurrentUserService _currentUserService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IFileStorageService _fileStorageService;

    public AttachmentService(
        IRepository<Request> requestRepository,
        IRepository<RequestAttachment> attachmentRepository,
        IRepository<User> userRepository,
        ICurrentUserService currentUserService,
        IUnitOfWork unitOfWork,
        IFileStorageService fileStorageService)
    {
        _requestRepository = requestRepository;
        _attachmentRepository = attachmentRepository;
        _userRepository = userRepository;
        _currentUserService = currentUserService;
        _unitOfWork = unitOfWork;
        _fileStorageService = fileStorageService;
    }

    public async Task<AttachmentResponseDto> UploadAsync(
        int requestId,
        FileUploadDto file,
        CancellationToken cancellationToken = default)
    {
        ValidateFile(file);

        var request = await _requestRepository.GetByIdAsync(requestId, cancellationToken)
            ?? throw new NotFoundAppException("Talep bulunamadı.");

        var currentUserId = RequireCurrentUserId();

        // Yorumlardan (FR-38) farklı olarak yalnızca talep sahibi dosya ekleyebilir
        // (örn. masraf fişi) — yönetici yalnızca görüntüleyip indirebilir (bkz. DownloadAsync).
        if (request.RequesterId != currentUserId)
        {
            throw new ForbiddenAppException("Bu talebe yalnızca talep sahibi dosya ekleyebilir.");
        }

        // Sonuçlanmış (Approved/Rejected/Cancelled) bir talebe sonradan kanıt eklenmesi
        // anlamsız — FR-22'deki düzenleme penceresiyle tutarlı bir kısıtlama.
        if (request.Status is not (RequestStatus.Draft or RequestStatus.Pending))
        {
            throw new ConflictAppException("Yalnızca taslak veya beklemede olan taleplere dosya eklenebilir.");
        }

        var storageKey = await _fileStorageService.SaveAsync(
            requestId, file.FileName, file.Content, cancellationToken);

        var attachment = new RequestAttachment
        {
            RequestId = requestId,
            FileName = file.FileName,
            FilePath = storageKey,
            ContentType = file.ContentType,
            FileSize = file.Length,
            UploadedAt = DateTime.UtcNow
        };

        await _attachmentRepository.AddAsync(attachment, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new AttachmentResponseDto(
            attachment.Id, attachment.FileName, attachment.ContentType, attachment.FileSize, attachment.UploadedAt);
    }

    public async Task<AttachmentDownloadResult> DownloadAsync(
        int requestId,
        int attachmentId,
        CancellationToken cancellationToken = default)
    {
        var request = await _requestRepository.GetByIdAsync(requestId, cancellationToken)
            ?? throw new NotFoundAppException("Talep bulunamadı.");

        var currentUserId = RequireCurrentUserId();
        var isOwner = request.RequesterId == currentUserId;

        var isManagerOfRequester = false;
        if (!isOwner)
        {
            var requester = await _userRepository.GetByIdAsync(request.RequesterId, cancellationToken);
            isManagerOfRequester = requester is not null && requester.ManagerId == currentUserId;
        }

        // FR-26 ile aynı görünürlük kuralı: yalnızca talep sahibi ve ilgili yönetici indirebilir.
        if (!isOwner && !isManagerOfRequester)
        {
            throw new ForbiddenAppException("Bu dosyayı indirme yetkiniz yok.");
        }

        var attachment = await _attachmentRepository.FirstOrDefaultAsync(
                a => a.Id == attachmentId && a.RequestId == requestId, cancellationToken)
            ?? throw new NotFoundAppException("Dosya eki bulunamadı.");

        var stream = await _fileStorageService.OpenReadAsync(attachment.FilePath, cancellationToken);

        return new AttachmentDownloadResult
        {
            Content = stream,
            FileName = attachment.FileName,
            ContentType = attachment.ContentType
        };
    }

    private static void ValidateFile(FileUploadDto file)
    {
        var errors = new Dictionary<string, string[]>();

        if (file.Length <= 0)
        {
            errors["file"] = ["Yüklenecek bir dosya seçilmelidir."];
        }
        else if (file.Length > MaxFileSizeBytes)
        {
            errors["file"] = [$"Dosya boyutu en fazla {MaxFileSizeBytes / (1024 * 1024)} MB olabilir."];
        }
        else
        {
            var extension = Path.GetExtension(file.FileName);
            if (string.IsNullOrEmpty(extension) || !AllowedExtensions.Contains(extension))
            {
                errors["file"] = [$"İzin verilen dosya türleri: {string.Join(", ", AllowedExtensions.OrderBy(x => x))}"];
            }
        }

        if (errors.Count > 0)
        {
            throw new ValidationAppException(errors);
        }
    }

    private int RequireCurrentUserId()
    {
        return _currentUserService.UserId
            ?? throw new UnauthorizedAppException("Geçerli bir token gerekli.");
    }
}
