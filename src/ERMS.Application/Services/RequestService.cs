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
    private readonly IRequestQueryRepository _requestQueryRepository;
    private readonly ICurrentUserService _currentUserService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IValidator<CreateRequestDto> _createValidator;
    private readonly IValidator<UpdateRequestDto> _updateValidator;

    public RequestService(
        IRepository<Request> requestRepository,
        IRepository<RequestType> requestTypeRepository,
        IRequestQueryRepository requestQueryRepository,
        ICurrentUserService currentUserService,
        IUnitOfWork unitOfWork,
        IValidator<CreateRequestDto> createValidator,
        IValidator<UpdateRequestDto> updateValidator)
    {
        _requestRepository = requestRepository;
        _requestTypeRepository = requestTypeRepository;
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
