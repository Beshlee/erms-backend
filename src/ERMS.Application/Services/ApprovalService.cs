using ERMS.Application.Abstractions.Authentication;
using ERMS.Application.Abstractions.Persistence;
using ERMS.Application.Common.Exceptions;
using ERMS.Application.DTOs.Approvals;
using ERMS.Application.Interfaces;
using ERMS.Domain.Entities;
using ERMS.Domain.Enums;
using FluentValidation;

namespace ERMS.Application.Services;

/// <summary>
/// Yönetici onay/red akışı (FR-32..37) — ApproveAsync ve RejectAsync, ortak kuralları
/// (kendi talebini onaylayamama, yalnızca kendine bağlı personeli onaylayabilme, zaten
/// sonuçlanmış talebi tekrar karara bağlayamama) paylaşan private DecideAsync'i çağırır.
/// </summary>
public sealed class ApprovalService : IApprovalService
{
    private readonly IRepository<Request> _requestRepository;
    private readonly IRepository<Approval> _approvalRepository;
    private readonly IRepository<RequestHistory> _requestHistoryRepository;
    private readonly IRepository<User> _userRepository;
    private readonly IRequestQueryRepository _requestQueryRepository;
    private readonly ICurrentUserService _currentUserService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IValidator<ApproveRequestDto> _approveValidator;
    private readonly IValidator<RejectRequestDto> _rejectValidator;

    public ApprovalService(
        IRepository<Request> requestRepository,
        IRepository<Approval> approvalRepository,
        IRepository<RequestHistory> requestHistoryRepository,
        IRepository<User> userRepository,
        IRequestQueryRepository requestQueryRepository,
        ICurrentUserService currentUserService,
        IUnitOfWork unitOfWork,
        IValidator<ApproveRequestDto> approveValidator,
        IValidator<RejectRequestDto> rejectValidator)
    {
        _requestRepository = requestRepository;
        _approvalRepository = approvalRepository;
        _requestHistoryRepository = requestHistoryRepository;
        _userRepository = userRepository;
        _requestQueryRepository = requestQueryRepository;
        _currentUserService = currentUserService;
        _unitOfWork = unitOfWork;
        _approveValidator = approveValidator;
        _rejectValidator = rejectValidator;
    }

    public async Task<List<PendingApprovalItemDto>> GetPendingApprovalsAsync(
        CancellationToken cancellationToken = default)
    {
        var currentUserId = RequireCurrentUserId();

        // FR-32: yalnızca yöneticiye bağlı (ManagerId) personelin bekleyen talepleri
        // (RequestQueryRepository.GetPendingManagerRequestsAsync Gün 1-2'de hazırlanmıştı).
        var requests = await _requestQueryRepository.GetPendingManagerRequestsAsync(currentUserId, cancellationToken);

        return requests
            .Select(r => new PendingApprovalItemDto(
                r.Id,
                r.Title,
                $"{r.Requester.FirstName} {r.Requester.LastName}",
                r.RequestType.Name,
                r.Status.ToString(),
                r.CreatedAt))
            .ToList();
    }

    public async Task<ApprovalResultDto> ApproveAsync(
        int requestId,
        ApproveRequestDto dto,
        CancellationToken cancellationToken = default)
    {
        await ValidateAsync(_approveValidator, dto, cancellationToken);

        return await DecideAsync(
            requestId, dto.Comment, ApprovalDecision.Approved, RequestStatus.Approved, cancellationToken);
    }

    public async Task<ApprovalResultDto> RejectAsync(
        int requestId,
        RejectRequestDto dto,
        CancellationToken cancellationToken = default)
    {
        // FR-34: gerekçe zorunlu — FluentValidation NotEmpty ile denetleniyor.
        await ValidateAsync(_rejectValidator, dto, cancellationToken);

        return await DecideAsync(
            requestId, dto.Comment, ApprovalDecision.Rejected, RequestStatus.Rejected, cancellationToken);
    }

    private async Task<ApprovalResultDto> DecideAsync(
        int requestId,
        string? comment,
        ApprovalDecision decision,
        RequestStatus newStatus,
        CancellationToken cancellationToken)
    {
        var request = await _requestRepository.GetByIdAsync(requestId, cancellationToken)
            ?? throw new NotFoundAppException("Talep bulunamadı.");

        var currentUserId = RequireCurrentUserId();

        // FR-36: bir kullanıcı kendi oluşturduğu talebi onaylayamaz/reddedemez.
        if (request.RequesterId == currentUserId)
        {
            throw new ForbiddenAppException("Kendi talebinizi onaylayamaz/reddedemezsiniz.");
        }

        var requester = await _userRepository.GetByIdAsync(request.RequesterId, cancellationToken);

        // FR-32 / US-06: yalnızca kendisine bağlı personelin talebi üzerinde karar verebilir.
        if (requester is null || requester.ManagerId != currentUserId)
        {
            throw new ForbiddenAppException("Bu personel size bağlı değil.");
        }

        // FR-37: zaten sonuçlanmış (Approved/Rejected/Cancelled) bir talep tekrar karara bağlanamaz.
        if (request.Status != RequestStatus.Pending)
        {
            throw new ConflictAppException("Bu talep zaten sonuçlanmış ya da onay bekleyen durumda değil.");
        }

        var oldStatus = request.Status;
        request.Status = newStatus;
        request.UpdatedAt = DateTime.UtcNow;
        _requestRepository.Update(request);

        // FR-35: her onay/red işlemi için bir Approval kaydı oluşturulmalı.
        var decidedAt = DateTime.UtcNow;
        var approval = new Approval
        {
            RequestId = requestId,
            ApproverId = currentUserId,
            Decision = decision,
            Comment = comment,
            DecidedAt = decidedAt
        };
        await _approvalRepository.AddAsync(approval, cancellationToken);

        // FR-41: bu da bir durum değişikliği — audit log'a yazılır.
        var note = decision == ApprovalDecision.Approved ? "Talep onaylandı." : "Talep reddedildi.";
        await AddHistoryAsync(requestId, currentUserId, oldStatus, newStatus, note, cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var approver = await _userRepository.GetByIdAsync(currentUserId, cancellationToken);
        var approverName = approver is not null ? $"{approver.FirstName} {approver.LastName}" : "Bilinmiyor";

        return new ApprovalResultDto(requestId, newStatus.ToString(), approverName, decidedAt);
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
