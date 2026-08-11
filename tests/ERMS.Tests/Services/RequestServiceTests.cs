using System.Linq.Expressions;
using ERMS.Application.Abstractions.Authentication;
using ERMS.Application.Abstractions.Persistence;
using ERMS.Application.Common.Exceptions;
using ERMS.Application.DTOs.Requests;
using ERMS.Application.Services;
using ERMS.Application.Validators;
using ERMS.Domain.Entities;
using ERMS.Domain.Enums;
using Moq;

namespace ERMS.Tests.Services;

/// <summary>
/// RequestService — talep oluşturma/güncelleme/iptal iş kuralları (FR-18..22, FR-30).
/// DTO validator'lar (CreateRequestDtoValidator/UpdateRequestDtoValidator) gerçek
/// örnekleriyle kullanılıyor — yalnızca repository/unit-of-work/current-user mock'lanıyor.
/// </summary>
public class RequestServiceTests
{
    private const int CurrentUserId = 10;

    private static RequestType LeaveType(bool requiresApproval = true) => new()
    {
        Id = 1,
        Name = "İzin",
        RequiresApproval = requiresApproval,
        IsActive = true
    };

    private static RequestType ExpenseType(bool requiresApproval = true) => new()
    {
        Id = 2,
        Name = "Masraf",
        RequiresApproval = requiresApproval,
        IsActive = true
    };

    private static Mock<ICurrentUserService> CurrentUserMock(int userId = CurrentUserId)
    {
        var mock = new Mock<ICurrentUserService>();
        mock.SetupGet(c => c.UserId).Returns(userId);
        mock.SetupGet(c => c.Role).Returns(UserRole.Employee);
        return mock;
    }

    private static RequestService CreateSut(
        Mock<IRepository<Request>> requestRepo,
        Mock<IRepository<RequestType>> requestTypeRepo,
        Mock<ICurrentUserService> currentUser,
        Mock<IUnitOfWork>? unitOfWork = null)
    {
        unitOfWork ??= new Mock<IUnitOfWork>();
        unitOfWork.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        return new RequestService(
            requestRepo.Object,
            requestTypeRepo.Object,
            Mock.Of<IRepository<RequestHistory>>(),
            Mock.Of<IRequestQueryRepository>(),
            currentUser.Object,
            unitOfWork.Object,
            new CreateRequestDtoValidator(),
            new UpdateRequestDtoValidator());
    }

    private static Mock<IRepository<RequestType>> RequestTypeRepoReturning(RequestType type)
    {
        var repo = new Mock<IRepository<RequestType>>();
        repo.Setup(r => r.FirstOrDefaultAsync(
                It.IsAny<Expression<Func<RequestType, bool>>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(type);
        repo.Setup(r => r.GetByIdAsync(type.Id, It.IsAny<CancellationToken>())).ReturnsAsync(type);
        return repo;
    }

    // --- CreateAsync ---------------------------------------------------

    [Fact]
    public async Task CreateAsync_LeaveTypeSubmittedWithoutDates_ThrowsValidation()
    {
        // FR-18: İzin türü GÖNDERİLİRKEN (taslak değil) başlangıç/bitiş tarihi zorunlu.
        var requestTypeRepo = RequestTypeRepoReturning(LeaveType());
        var requestRepo = new Mock<IRepository<Request>>();
        var sut = CreateSut(requestRepo, requestTypeRepo, CurrentUserMock());

        var dto = new CreateRequestDto(
            RequestTypeId: 1,
            Title: "Yıllık izin talebi",
            Description: "Ailevi sebep",
            StartDate: null,
            EndDate: null,
            Amount: null,
            Priority: RequestPriority.Normal,
            SaveAsDraft: false);

        var ex = await Assert.ThrowsAsync<ValidationAppException>(() => sut.CreateAsync(dto));

        Assert.Contains("startDate", ex.Errors.Keys);
        Assert.Contains("endDate", ex.Errors.Keys);
    }

    [Fact]
    public async Task CreateAsync_SaveAsDraftTrue_SkipsTypeSpecificValidation()
    {
        // FR-21: taslak, eksik bilgiyle kaydedilebilir — tarih zorunluluğu uygulanmamalı.
        var requestTypeRepo = RequestTypeRepoReturning(LeaveType());
        var requestRepo = new Mock<IRepository<Request>>();
        var sut = CreateSut(requestRepo, requestTypeRepo, CurrentUserMock());

        var dto = new CreateRequestDto(
            RequestTypeId: 1,
            Title: "Yıllık izin talebi",
            Description: "Ailevi sebep",
            StartDate: null,
            EndDate: null,
            Amount: null,
            Priority: RequestPriority.Normal,
            SaveAsDraft: true);

        var result = await sut.CreateAsync(dto);

        Assert.Equal("Draft", result.Status);
        requestRepo.Verify(r => r.AddAsync(It.IsAny<Request>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_ExpenseTypeWithoutAmount_NotDraft_ThrowsValidation()
    {
        // FR-19: Masraf türü GÖNDERİLİRKEN tutar zorunlu.
        var requestTypeRepo = RequestTypeRepoReturning(ExpenseType());
        var requestRepo = new Mock<IRepository<Request>>();
        var sut = CreateSut(requestRepo, requestTypeRepo, CurrentUserMock());

        var dto = new CreateRequestDto(
            RequestTypeId: 2,
            Title: "Yol masrafı",
            Description: "Müşteri ziyareti",
            StartDate: null,
            EndDate: null,
            Amount: null,
            Priority: RequestPriority.Normal,
            SaveAsDraft: false);

        var ex = await Assert.ThrowsAsync<ValidationAppException>(() => sut.CreateAsync(dto));

        Assert.Contains("amount", ex.Errors.Keys);
    }

    [Fact]
    public async Task CreateAsync_EndDateBeforeStartDate_ThrowsValidation()
    {
        // FR-20: bitiş tarihi başlangıçtan önce olamaz (DTO validator seviyesinde yakalanır).
        var requestTypeRepo = RequestTypeRepoReturning(LeaveType());
        var requestRepo = new Mock<IRepository<Request>>();
        var sut = CreateSut(requestRepo, requestTypeRepo, CurrentUserMock());

        var dto = new CreateRequestDto(
            RequestTypeId: 1,
            Title: "Yıllık izin talebi",
            Description: "Ailevi sebep",
            StartDate: new DateTime(2026, 8, 20),
            EndDate: new DateTime(2026, 8, 10),
            Amount: null,
            Priority: RequestPriority.Normal,
            SaveAsDraft: false);

        await Assert.ThrowsAsync<ValidationAppException>(() => sut.CreateAsync(dto));
    }

    [Fact]
    public async Task CreateAsync_TypeNotFoundOrInactive_ThrowsNotFound()
    {
        // FR-15: pasif (ya da olmayan) bir talep türü seçilemez.
        var requestTypeRepo = new Mock<IRepository<RequestType>>();
        requestTypeRepo
            .Setup(r => r.FirstOrDefaultAsync(
                It.IsAny<Expression<Func<RequestType, bool>>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((RequestType?)null);

        var requestRepo = new Mock<IRepository<Request>>();
        var sut = CreateSut(requestRepo, requestTypeRepo, CurrentUserMock());

        var dto = new CreateRequestDto(
            RequestTypeId: 99,
            Title: "Talep",
            Description: "Açıklama",
            StartDate: null,
            EndDate: null,
            Amount: null,
            Priority: RequestPriority.Normal,
            SaveAsDraft: true);

        await Assert.ThrowsAsync<NotFoundAppException>(() => sut.CreateAsync(dto));
    }

    // --- UpdateAsync -----------------------------------------------------

    [Fact]
    public async Task UpdateAsync_NotOwner_ThrowsForbidden()
    {
        // FR-22: yalnızca talep sahibi düzenleyebilir.
        var existing = new Request { Id = 5, RequesterId = 999, Status = RequestStatus.Draft, Title = "x", Description = "y" };
        var requestRepo = new Mock<IRepository<Request>>();
        requestRepo.Setup(r => r.GetByIdAsync(5, It.IsAny<CancellationToken>())).ReturnsAsync(existing);

        var sut = CreateSut(requestRepo, RequestTypeRepoReturning(LeaveType()), CurrentUserMock());

        var dto = new UpdateRequestDto(1, "Yeni başlık", "Yeni açıklama", null, null, null, RequestPriority.Normal);

        await Assert.ThrowsAsync<ForbiddenAppException>(() => sut.UpdateAsync(5, dto));
    }

    [Fact]
    public async Task UpdateAsync_NonDraftStatus_ThrowsConflict()
    {
        // PUT yalnızca taslak durumundaki talepleri günceller.
        var existing = new Request { Id = 5, RequesterId = CurrentUserId, Status = RequestStatus.Pending, Title = "x", Description = "y" };
        var requestRepo = new Mock<IRepository<Request>>();
        requestRepo.Setup(r => r.GetByIdAsync(5, It.IsAny<CancellationToken>())).ReturnsAsync(existing);

        var sut = CreateSut(requestRepo, RequestTypeRepoReturning(LeaveType()), CurrentUserMock());

        var dto = new UpdateRequestDto(1, "Yeni başlık", "Yeni açıklama", null, null, null, RequestPriority.Normal);

        await Assert.ThrowsAsync<ConflictAppException>(() => sut.UpdateAsync(5, dto));
    }

    // --- CancelAsync -------------------------------------------------

    [Fact]
    public async Task CancelAsync_NonPendingStatus_ThrowsConflict()
    {
        // FR-30: yalnızca Pending durumundaki talepler iptal edilebilir.
        var existing = new Request { Id = 7, RequesterId = CurrentUserId, Status = RequestStatus.Approved, Title = "x", Description = "y" };
        var requestRepo = new Mock<IRepository<Request>>();
        requestRepo.Setup(r => r.GetByIdAsync(7, It.IsAny<CancellationToken>())).ReturnsAsync(existing);

        var sut = CreateSut(requestRepo, RequestTypeRepoReturning(LeaveType()), CurrentUserMock());

        await Assert.ThrowsAsync<ConflictAppException>(() => sut.CancelAsync(7));
    }

    [Fact]
    public async Task CancelAsync_PendingRequestOwnedByCaller_TransitionsToCancelled()
    {
        var leaveType = LeaveType();
        var existing = new Request
        {
            Id = 7,
            RequesterId = CurrentUserId,
            RequestTypeId = leaveType.Id,
            Status = RequestStatus.Pending,
            Title = "İzin talebi",
            Description = "y"
        };
        var requestRepo = new Mock<IRepository<Request>>();
        requestRepo.Setup(r => r.GetByIdAsync(7, It.IsAny<CancellationToken>())).ReturnsAsync(existing);

        var sut = CreateSut(requestRepo, RequestTypeRepoReturning(leaveType), CurrentUserMock());

        var result = await sut.CancelAsync(7);

        Assert.Equal("Cancelled", result.Status);
        Assert.Equal(RequestStatus.Cancelled, existing.Status);
        requestRepo.Verify(r => r.Update(existing), Times.Once);
    }
}
