using ERMS.Application.Abstractions.Authentication;
using ERMS.Application.Abstractions.Persistence;
using ERMS.Application.Common.Exceptions;
using ERMS.Application.Common.Models;
using ERMS.Application.DTOs.Requests;
using ERMS.Application.Interfaces;
using ERMS.Domain.Entities;
using ERMS.Domain.Enums;
using FluentValidation;

namespace ERMS.Application.Services;

public sealed class RequestService : IRequestService
{
    // Bölüm 4.2/FR-13 varsayılan türleri isimle eşleştiriyor — doküman RequestType'a
    // "kategori" alanı tanımlamadığı için (yalnızca Name/RequiresApproval/IsActive var),
    // tür bazlı kurallar (FR-18/FR-19) isim eşleşmesiyle uygulanıyor. Admin ileride
    // "İzin"/"Masraf" dışında bir isimle yeni tür eklerse bu kurallar ona uygulanmaz —
    // bilinçli bir sınırlama, ileride RequestType'a bir Category alanı eklenerek
    // sağlamlaştırılabilir.
    private const string LeaveTypeName = "İzin";
    private const string ExpenseTypeName = "Masraf";

    private readonly IRepository<Request> _requestRepository;
    private readonly IRepository<RequestType> _requestTypeRepository;
    private readonly IRepository<RequestHistory> _requestHistoryRepository;
    private readonly IRequestQueryRepository _requestQueryRepository;
    private readonly ICurrentUserService _currentUserService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IValidator<CreateRequestDto> _createValidator;
    private readonly IValidator<UpdateRequestDto> _updateValidator;

    public RequestService(
        IRepository<Request> requestRepository,
        IRepository<RequestType> requestTypeRepository,
        IRepository<RequestHistory> requestHistoryRepository,
        IRequestQueryRepository requestQueryRepository,
        ICurrentUserService currentUserService,
        IUnitOfWork unitOfWork,
        IValidator<CreateRequestDto> createValidator,
        IValidator<UpdateRequestDto> updateValidator)
    {
        _requestRepository = requestRepository;
        _requestTypeRepository = requestTypeRepository;
        _requestHistoryRepository = requestHistoryRepository;
        _requestQueryRepository = requestQueryRepository;
        _currentUserService = currentUserService;
        _unitOfWork = unitOfWork;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
    }

    public async Task<RequestResponseDto> CreateAsync(
        CreateRequestDto dto,
        CancellationToken cancellationToken = default)
    {
        await ValidateAsync(_createValidator, dto, cancellationToken);

        var requestType = await GetActiveRequestTypeAsync(dto.RequestTypeId, cancellationToken);

        // FR-21 / US-03: taslak, "eksik bilgili" olabilir — tür bazlı zorunluluklar
        // (FR-18 tarih, FR-19 tutar) yalnızca gerçekten gönderilirken uygulanır.
        if (!dto.SaveAsDraft)
        {
            ValidateTypeSpecificRules(requestType, dto.StartDate, dto.EndDate, dto.Amount);
        }

        var currentUserId = RequireCurrentUserId();
        var now = DateTime.UtcNow;

        // FR-21: taslak olarak kaydedilebilir. FR-23/FR-24: taslak değilse, türün onay
        // gerekip gerekmediğine göre doğrudan Pending ya da Approved olur.
        var status = dto.SaveAsDraft
            ? RequestStatus.Draft
            : requestType.RequiresApproval
                ? RequestStatus.Pending
                : RequestStatus.Approved;

        var request = new Request
        {
            RequestTypeId = requestType.Id,
            RequesterId = currentUserId,
            Title = dto.Title,
            Description = dto.Description,
            Status = status,
            Priority = dto.Priority,
            StartDate = dto.StartDate,
            EndDate = dto.EndDate,
            Amount = dto.Amount,
            CreatedAt = now,
            UpdatedAt = now
        };

        await _requestRepository.AddAsync(request, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // FR-41: Draft dışında bir durumla doğrudan oluşturulduysa (saveAsDraft=false),
        // bu da ilk durum değişikliğidir — wireframe'deki "Oluşturuldu (Taslak → Beklemede)"
        // örneğiyle aynı mantık (Şekil 3, Ekran 4). request.Id'ye ihtiyaç duyduğu için
        // yukarıdaki SaveChangesAsync'ten sonra, ayrı bir kayıt olarak yapılıyor.
        if (status != RequestStatus.Draft)
        {
            await AddHistoryAsync(
                request.Id, currentUserId, RequestStatus.Draft, status, "Talep oluşturuldu ve gönderildi.", cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }

        return new RequestResponseDto(
            request.Id,
            request.Title,
            requestType.Name,
            request.Status.ToString(),
            request.CreatedAt);
    }

    public async Task<RequestResponseDto> UpdateAsync(
        int requestId,
        UpdateRequestDto dto,
        CancellationToken cancellationToken = default)
    {
        await ValidateAsync(_updateValidator, dto, cancellationToken);

        // Tracked entity gerekiyor (Update sonrası SaveChanges) — bkz. Repository<T> yorumu.
        var request = await _requestRepository.GetByIdAsync(requestId, cancellationToken)
            ?? throw new NotFoundAppException("Talep bulunamadı.");

        var currentUserId = RequireCurrentUserId();

        // FR-22 / US-03: yalnızca talep sahibi düzenleyebilir.
        if (request.RequesterId != currentUserId)
        {
            throw new ForbiddenAppException("Bu talep size ait değil.");
        }

        // PUT yalnızca taslakları günceller — durum değişimi ayrı bir uç noktadır (Gün 6).
        if (request.Status != RequestStatus.Draft)
        {
            throw new ConflictAppException("Yalnızca taslak durumundaki talepler düzenlenebilir.");
        }

        var requestType = await GetActiveRequestTypeAsync(dto.RequestTypeId, cancellationToken);

        // PUT sonrası talep hâlâ Draft'ta kalır (durum değişmez) — bu yüzden burada da
        // FR-18/FR-19'un tür bazlı zorunluluğu uygulanmaz, CreateAsync'teki gerekçeyle aynı.

        request.RequestTypeId = requestType.Id;
        request.Title = dto.Title;
        request.Description = dto.Description;
        request.Priority = dto.Priority;
        request.StartDate = dto.StartDate;
        request.EndDate = dto.EndDate;
        request.Amount = dto.Amount;
        request.UpdatedAt = DateTime.UtcNow;

        _requestRepository.Update(request);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new RequestResponseDto(
            request.Id,
            request.Title,
            requestType.Name,
            request.Status.ToString(),
            request.CreatedAt);
    }

    public async Task<PagedResult<RequestResponseDto>> GetMyRequestsAsync(
        string? status,
        int? requestTypeId,
        string? search,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var currentUserId = RequireCurrentUserId();

        // FR-27: durum filtresi metin olarak geliyor (?status=Pending), enum'a çeviriyoruz.
        RequestStatus? parsedStatus = null;
        if (!string.IsNullOrWhiteSpace(status))
        {
            if (!Enum.TryParse<RequestStatus>(status, ignoreCase: true, out var parsed))
            {
                throw new ValidationAppException(new Dictionary<string, string[]>
                {
                    ["status"] = ["Geçersiz durum değeri. Beklenen: Draft, Pending, Approved, Rejected, Cancelled."]
                });
            }

            parsedStatus = parsed;
        }

        var query = new RequestQuery
        {
            Status = parsedStatus,
            RequestTypeId = requestTypeId,
            Search = search,
            Page = page < 1 ? 1 : page,
            // FR-28: sayfalama — kötüye kullanımı (pageSize=100000 gibi) önlemek için üst sınır.
            PageSize = pageSize is < 1 or > 100 ? 10 : pageSize
        };

        // FR-25/26: yalnızca giriş yapan kullanıcının kendi talepleri (currentUserId ile sınırlı).
        // FR-29: başlık arama da bu sorgunun içinde (RequestQueryRepository'de Title.Contains).
        var paged = await _requestQueryRepository.GetEmployeeRequestsAsync(currentUserId, query, cancellationToken);

        var items = paged.Items
            .Select(r => new RequestResponseDto(r.Id, r.Title, r.RequestType.Name, r.Status.ToString(), r.CreatedAt))
            .ToList();

        return new PagedResult<RequestResponseDto>
        {
            Items = items,
            Page = paged.Page,
            PageSize = paged.PageSize,
            TotalCount = paged.TotalCount
        };
    }

    public async Task<RequestResponseDto> SubmitAsync(
        int requestId,
        CancellationToken cancellationToken = default)
    {
        var request = await _requestRepository.GetByIdAsync(requestId, cancellationToken)
            ?? throw new NotFoundAppException("Talep bulunamadı.");

        var currentUserId = RequireCurrentUserId();

        if (request.RequesterId != currentUserId)
        {
            throw new ForbiddenAppException("Bu talep size ait değil.");
        }

        if (request.Status != RequestStatus.Draft)
        {
            throw new ConflictAppException("Yalnızca taslak durumundaki talepler gönderilebilir.");
        }

        var requestType = await GetActiveRequestTypeAsync(request.RequestTypeId, cancellationToken);

        // Taslakken eksik bırakılabilen tür bazlı alanlar (FR-18/19) artık tamam olmalı —
        // "gönderme" tam da bu tamlığın denetlendiği andır (bkz. CreateAsync'teki gerekçe).
        ValidateTypeSpecificRules(requestType, request.StartDate, request.EndDate, request.Amount);

        var oldStatus = request.Status;

        // FR-23/FR-24: onay gerektirmeyen türlerde doğrudan Approved, diğerlerinde Pending.
        var newStatus = requestType.RequiresApproval
            ? RequestStatus.Pending
            : RequestStatus.Approved;

        request.Status = newStatus;
        request.UpdatedAt = DateTime.UtcNow;
        _requestRepository.Update(request);

        await AddHistoryAsync(request.Id, currentUserId, oldStatus, newStatus, "Talep gönderildi.", cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new RequestResponseDto(
            request.Id,
            request.Title,
            requestType.Name,
            request.Status.ToString(),
            request.CreatedAt);
    }

    public async Task<RequestResponseDto> CancelAsync(
        int requestId,
        CancellationToken cancellationToken = default)
    {
        var request = await _requestRepository.GetByIdAsync(requestId, cancellationToken)
            ?? throw new NotFoundAppException("Talep bulunamadı.");

        var currentUserId = RequireCurrentUserId();

        if (request.RequesterId != currentUserId)
        {
            throw new ForbiddenAppException("Bu talep size ait değil.");
        }

        // FR-30 / US-05: yalnızca Pending durumundaki talep iptal edilebilir
        // (Approved bir talebi iptal etmeye çalışmak reddedilir).
        if (request.Status != RequestStatus.Pending)
        {
            throw new ConflictAppException("Yalnızca beklemede olan talepler iptal edilebilir.");
        }

        var oldStatus = request.Status;
        request.Status = RequestStatus.Cancelled;
        request.UpdatedAt = DateTime.UtcNow;
        _requestRepository.Update(request);

        await AddHistoryAsync(request.Id, currentUserId, oldStatus, RequestStatus.Cancelled, "Talep iptal edildi.", cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // Not: Cancel'da GetActiveRequestTypeAsync kullanmıyoruz — tür sonradan pasife
        // alınmış olsa bile mevcut bir talebi iptal edebilmek gerekir (FR-15 yalnızca
        // "yeni talep oluştururken" seçilebilirliği kısıtlıyor).
        var requestType = await _requestTypeRepository.GetByIdAsync(request.RequestTypeId, cancellationToken);

        return new RequestResponseDto(
            request.Id,
            request.Title,
            requestType?.Name ?? "Bilinmiyor",
            request.Status.ToString(),
            request.CreatedAt);
    }

    public async Task<RequestDetailDto> GetDetailAsync(
        int requestId,
        CancellationToken cancellationToken = default)
    {
        var request = await _requestQueryRepository.GetDetailAsync(requestId, cancellationToken)
            ?? throw new NotFoundAppException("Talep bulunamadı.");

        var currentUserId = RequireCurrentUserId();
        var currentUserRole = _currentUserService.Role;

        var isOwner = request.RequesterId == currentUserId;
        var isManagerOfRequester = currentUserRole == UserRole.Manager
            && request.Requester.ManagerId == currentUserId;

        // FR-26: Employee yalnızca kendi talebini görebilir; Manager kendisine bağlı
        // personelin talebini de görebilir (Bölüm 1.4).
        if (!isOwner && !isManagerOfRequester)
        {
            throw new ForbiddenAppException("Bu talebi görüntüleme yetkiniz yok.");
        }

        // FR-42: durum geçmişi kronolojik sırada (RequestQueryRepository zaten ChangedAt'e göre sıralıyor).
        var history = request.History
            .Select(h => new RequestHistoryItemDto(
                h.OldStatus.ToString(),
                h.NewStatus.ToString(),
                h.Note,
                h.ChangedAt,
                $"{h.ChangedBy.FirstName} {h.ChangedBy.LastName}"))
            .ToList();

        // FR-39: yorumlar, yazan kişi ve oluşturulma zamanıyla birlikte, kronolojik sırada.
        var comments = request.Comments
            .OrderBy(c => c.CreatedAt)
            .Select(c => new CommentResponseDto(
                c.Id,
                c.Content,
                $"{c.Author.FirstName} {c.Author.LastName}",
                c.CreatedAt))
            .ToList();

        return new RequestDetailDto(
            request.Id,
            request.Title,
            request.Description,
            request.RequestType.Name,
            request.Status.ToString(),
            request.Priority.ToString(),
            request.StartDate,
            request.EndDate,
            request.Amount,
            request.CreatedAt,
            request.UpdatedAt,
            request.RequesterId,
            $"{request.Requester.FirstName} {request.Requester.LastName}",
            history,
            comments);
    }

    private async Task AddHistoryAsync(
        int requestId,
        int changedById,
        RequestStatus oldStatus,
        RequestStatus newStatus,
        string? note,
        CancellationToken cancellationToken)
    {
        var history = new RequestHistory
        {
            RequestId = requestId,
            ChangedById = changedById,
            OldStatus = oldStatus,
            NewStatus = newStatus,
            Note = note,
            ChangedAt = DateTime.UtcNow
        };

        await _requestHistoryRepository.AddAsync(history, cancellationToken);
    }

    private async Task<RequestType> GetActiveRequestTypeAsync(int requestTypeId, CancellationToken cancellationToken)
    {
        var requestType = await _requestTypeRepository.FirstOrDefaultAsync(
            x => x.Id == requestTypeId && x.IsActive,
            cancellationToken);

        // FR-15: pasif bir talep türü yeni talep oluşturulurken seçilebilir olmamalı.
        if (requestType is null)
        {
            throw new NotFoundAppException("Aktif bir talep türü bulunamadı.");
        }

        return requestType;
    }

    private static void ValidateTypeSpecificRules(
        RequestType requestType,
        DateTime? startDate,
        DateTime? endDate,
        decimal? amount)
    {
        var errors = new Dictionary<string, List<string>>();

        void AddError(string field, string message)
        {
            if (!errors.TryGetValue(field, out var list))
            {
                list = [];
                errors[field] = list;
            }

            list.Add(message);
        }

        // FR-18: İzin türünde başlangıç/bitiş tarihi zorunlu.
        if (requestType.Name.Equals(LeaveTypeName, StringComparison.OrdinalIgnoreCase))
        {
            if (startDate is null)
            {
                AddError("startDate", "İzin türü talepler için başlangıç tarihi zorunludur.");
            }

            if (endDate is null)
            {
                AddError("endDate", "İzin türü talepler için bitiş tarihi zorunludur.");
            }

            // FR-20: bitiş tarihi başlangıçtan önce olamaz.
            if (startDate is not null && endDate is not null && endDate < startDate)
            {
                AddError("endDate", "Bitiş tarihi başlangıç tarihinden önce olamaz.");
            }
        }

        // FR-19: Masraf türünde tutar zorunlu ve pozitif olmalı (pozitiflik FluentValidation'da).
        if (requestType.Name.Equals(ExpenseTypeName, StringComparison.OrdinalIgnoreCase) && amount is null)
        {
            AddError("amount", "Masraf türü talepler için tutar zorunludur.");
        }

        if (errors.Count > 0)
        {
            throw new ValidationAppException(
                errors.ToDictionary(x => x.Key, x => x.Value.ToArray()));
        }
    }

    private int RequireCurrentUserId()
    {
        return _currentUserService.UserId
            ?? throw new UnauthorizedAppException("Geçerli bir token gerekli.");
    }

    private static async Task ValidateAsync<T>(IValidator<T> validator, T instance, CancellationToken cancellationToken)
    {
        var result = await validator.ValidateAsync(instance, cancellationToken);

        if (!result.IsValid)
        {
            var errors = result.Errors
                .GroupBy(e => e.PropertyName)
                .ToDictionary(
                    g => char.ToLowerInvariant(g.Key[0]) + g.Key[1..],
                    g => g.Select(e => e.ErrorMessage).ToArray());

            throw new ValidationAppException(errors);
        }
    }
}
