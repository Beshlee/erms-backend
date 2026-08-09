using ERMS.Application.Abstractions.Authentication;
using ERMS.Application.Abstractions.Persistence;
using ERMS.Application.Common.Exceptions;
using ERMS.Application.DTOs.Requests;
using ERMS.Application.Interfaces;
using ERMS.Domain.Entities;
using FluentValidation;

namespace ERMS.Application.Services;

public sealed class CommentService : ICommentService
{
    private readonly IRepository<Request> _requestRepository;
    private readonly IRepository<User> _userRepository;
    private readonly IRepository<RequestComment> _commentRepository;
    private readonly ICurrentUserService _currentUserService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IValidator<CreateCommentDto> _validator;

    public CommentService(
        IRepository<Request> requestRepository,
        IRepository<User> userRepository,
        IRepository<RequestComment> commentRepository,
        ICurrentUserService currentUserService,
        IUnitOfWork unitOfWork,
        IValidator<CreateCommentDto> validator)
    {
        _requestRepository = requestRepository;
        _userRepository = userRepository;
        _commentRepository = commentRepository;
        _currentUserService = currentUserService;
        _unitOfWork = unitOfWork;
        _validator = validator;
    }

    public async Task<CommentResponseDto> AddCommentAsync(
        int requestId,
        CreateCommentDto dto,
        CancellationToken cancellationToken = default)
    {
        var result = await _validator.ValidateAsync(dto, cancellationToken);
        if (!result.IsValid)
        {
            var errors = result.Errors
                .GroupBy(e => e.PropertyName)
                .ToDictionary(
                    g => char.ToLowerInvariant(g.Key[0]) + g.Key[1..],
                    g => g.Select(e => e.ErrorMessage).ToArray());

            throw new ValidationAppException(errors);
        }

        var request = await _requestRepository.GetByIdAsync(requestId, cancellationToken)
            ?? throw new NotFoundAppException("Talep bulunamadı.");

        var currentUserId = _currentUserService.UserId
            ?? throw new UnauthorizedAppException("Geçerli bir token gerekli.");

        var isOwner = request.RequesterId == currentUserId;

        var isManagerOfRequester = false;
        if (!isOwner)
        {
            var requester = await _userRepository.GetByIdAsync(request.RequesterId, cancellationToken);
            isManagerOfRequester = requester is not null && requester.ManagerId == currentUserId;
        }

        // FR-38: talep sahibi ve ilgili yönetici talebe yorum ekleyebilir.
        if (!isOwner && !isManagerOfRequester)
        {
            throw new ForbiddenAppException("Bu talebe yorum ekleme yetkiniz yok.");
        }

        var comment = new RequestComment
        {
            RequestId = requestId,
            AuthorId = currentUserId,
            Content = dto.Content,
            CreatedAt = DateTime.UtcNow
        };

        await _commentRepository.AddAsync(comment, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // FR-39: yorum, yazan kişi ve zaman bilgisiyle döner.
        var author = await _userRepository.GetByIdAsync(currentUserId, cancellationToken);
        var authorName = author is not null ? $"{author.FirstName} {author.LastName}" : "Bilinmiyor";

        return new CommentResponseDto(comment.Id, comment.Content, authorName, comment.CreatedAt);
    }
}
